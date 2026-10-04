using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the MainMenu and Sandbox scenes and the build list. Scenes are not hand-edited as YAML in
/// this repo (see CLAUDE.md), so both are made by copying SampleScene through the AssetDatabase -
/// which keeps every GUID reference valid - and then editing the copy through the editor API.
///
/// The copies are snapshots: later edits to SampleScene do not reach them. Rerun the matching menu
/// item to re-copy. See docs/decisions.md #21.
///
/// Headless: Unity -batchmode -quit -executeMethod MainMenuBuilder.BuildAll
/// </summary>
public static class MainMenuBuilder
{
    const string ScenesDir = "Assets/Scenes/";
    // SampleScene, not GameSession.GameScene - that is the Arena now, and the menu backdrop and the
    // sandbox are copies of the open-terrain map.
    const string SourcePath = ScenesDir + "SampleScene.unity";
    static readonly string ArenaPath = ScenesDir + GameSession.GameScene + ".unity";
    static readonly string MenuPath = ScenesDir + GameSession.MainMenuScene + ".unity";
    static readonly string SandboxPath = ScenesDir + GameSession.SandboxScene + ".unity";

    const string ReturnToMenuName = "ReturnToMenu";

    [MenuItem("Tools/DroneMissile/Build All Menu Scenes")]
    public static void BuildAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // Order matters: SampleScene gets the stopgap first so the Sandbox copy inherits it.
        Scene source = EditorSceneManager.OpenScene(SourcePath, OpenSceneMode.Single);
        AddReturnToMenu();
        EditorSceneManager.SaveScene(source);

        BuildSandboxScene(false);
        BuildMainMenuScene(false);
        ConfigureBuildScenes();
    }

    [MenuItem("Tools/DroneMissile/Build Main Menu Scene")]
    public static void BuildMainMenuScene() => BuildMainMenuScene(true);

    static void BuildMainMenuScene(bool interactive)
    {
        if (interactive && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!CopyScene(MenuPath, interactive)) return;

        Scene scene = EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Single);

        // Gameplay out: the menu is a backdrop. A HUD would hunt for a drone, Esc would reload the
        // menu onto itself.
        DestroyByName("HUD");
        DestroyByName(ReturnToMenuName);

        GameObject drone = GameObject.FindGameObjectWithTag("Player");
        Camera cam = Camera.main;

        if (drone != null)
        {
            // DroneStunArcs [RequireComponent]s DroneControls, so it has to go first.
            RemoveComponent<DroneStunArcs>(drone);
            RemoveComponent<Shoting>(drone);
            RemoveComponent<DroneControls>(drone);
            RemoveComponent<Rigidbody>(drone);

            // No Player tag in the scene -> EnemyTurret never acquires a target and stays silent.
            drone.tag = "Untagged";
        }
        else
        {
            Debug.LogWarning("MainMenuBuilder: no Player-tagged drone in the copy; the backdrop orbits the origin.");
        }

        if (cam != null)
        {
            // The camera is a child of the drone in SampleScene. Out from under it, so it can circle.
            cam.transform.SetParent(null, true);
            MenuCameraOrbit orbit = cam.GetComponent<MenuCameraOrbit>();
            if (orbit == null) orbit = cam.gameObject.AddComponent<MenuCameraOrbit>();
            orbit.pivot = drone != null ? drone.transform : null;
        }
        else
        {
            Debug.LogWarning("MainMenuBuilder: no MainCamera-tagged camera in the copy; the menu will have no backdrop.");
        }

        new GameObject("MainMenu").AddComponent<MainMenu>();

        EditorSceneManager.SaveScene(scene);
        Debug.Log("MainMenuBuilder: built " + MenuPath + ".");
    }

    [MenuItem("Tools/DroneMissile/Build Sandbox Scene")]
    public static void BuildSandboxScene() => BuildSandboxScene(true);

    static void BuildSandboxScene(bool interactive)
    {
        if (interactive && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!CopyScene(SandboxPath, interactive)) return;

        Scene scene = EditorSceneManager.OpenScene(SandboxPath, OpenSceneMode.Single);
        AddReturnToMenu();
        AddConsoleToOpenScene();

        // The four turret types, spaced so they engage one at a time (docs/decisions.md #22).
        TurretTypesBuilder.PlaceInOpenSandbox();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("MainMenuBuilder: built " + SandboxPath + ".");
    }

    // The upgrade console (docs/plans/sandbox-console.md). Idempotent, and unlike a full rebuild it
    // leaves the rest of the Sandbox scene alone.
    [MenuItem("Tools/DroneMissile/Add Sandbox Console")]
    public static void AddSandboxConsole()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Scene scene = EditorSceneManager.OpenScene(SandboxPath, OpenSceneMode.Single);
        AddConsoleToOpenScene();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("MainMenuBuilder: SandboxConsole in " + SandboxPath + ".");
    }

    static void AddConsoleToOpenScene()
    {
        const string name = "SandboxConsole";
        GameObject go = GameObject.Find(name);
        if (go == null)
        {
            go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create SandboxConsole");
        }
        if (go.GetComponent<SandboxConsole>() == null) Undo.AddComponent<SandboxConsole>(go);
        EditorSceneManager.MarkSceneDirty(go.scene);
    }

    // TEMPORARY - see ReturnToMenu. Idempotent.
    [MenuItem("Tools/DroneMissile/Add ReturnToMenu (temporary Esc)")]
    public static void AddReturnToMenu()
    {
        GameObject go = GameObject.Find(ReturnToMenuName);
        if (go == null)
        {
            go = new GameObject(ReturnToMenuName);
            Undo.RegisterCreatedObjectUndo(go, "Create ReturnToMenu");
        }
        if (go.GetComponent<ReturnToMenu>() == null) Undo.AddComponent<ReturnToMenu>(go);

        EditorSceneManager.MarkSceneDirty(go.scene);
    }

    [MenuItem("Tools/DroneMissile/Configure Build Scenes")]
    public static void ConfigureBuildScenes()
    {
        // MainMenu first: index 0 is what a player build launches into.
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuPath, true),
            new EditorBuildSettingsScene(ArenaPath, true),
            new EditorBuildSettingsScene(SourcePath, true),
            new EditorBuildSettingsScene(SandboxPath, true),
        };
        Debug.Log("MainMenuBuilder: build scenes = MainMenu, Arena, SampleScene, Sandbox.");
    }

    // Fresh copy of SampleScene at `path`. An existing copy is replaced - after asking, when a
    // person is at the keyboard, because any hand tweaks made to it are lost.
    static bool CopyScene(string path, bool interactive)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
        {
            if (interactive && !EditorUtility.DisplayDialog("Rebuild scene",
                    path + " already exists. Replace it with a fresh copy of " + SourcePath + "?",
                    "Replace", "Cancel"))
            {
                return false;
            }
            AssetDatabase.DeleteAsset(path);
        }

        if (!AssetDatabase.CopyAsset(SourcePath, path))
        {
            Debug.LogError("MainMenuBuilder: could not copy " + SourcePath + " to " + path + ".");
            return false;
        }
        return true;
    }

    static void DestroyByName(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go != null) Object.DestroyImmediate(go);
    }

    // Every instance - the drone carries one DroneStunArcs per stun kind (docs/decisions.md #22).
    static void RemoveComponent<T>(GameObject go) where T : Component
    {
        foreach (T c in go.GetComponents<T>()) Object.DestroyImmediate(c);
    }
}
