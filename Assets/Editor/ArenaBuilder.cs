using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the Arena: the closed box the wave run happens in (docs/design/game-structure.md,
/// docs/decisions.md #23), plus the boss's assets. Scenes and prefabs are never hand-edited as YAML
/// in this repo (CLAUDE.md), so it all goes through the editor API.
///
/// The scene is a copy of SampleScene - which brings the drone, its camera, HUD, lighting and
/// post-processing with every reference intact - with the terrain, pad and turret taken out and the
/// box put in. Rerunning replaces the scene (after asking). The boss assets are created only if
/// missing, so Inspector tuning on them survives a rerun, like TurretTypesBuilder's.
///
/// Requires the turret types: run Tools > DroneMissile > Build Turret Types first.
/// Headless: Unity -batchmode -quit -executeMethod ArenaBuilder.BuildAll
/// </summary>
public static class ArenaBuilder
{
    const string SourceScene = "Assets/Scenes/SampleScene.unity";
    static readonly string ArenaPath = "Assets/Scenes/" + GameSession.GameScene + ".unity";

    const string TurretDir = "Assets/Prefabs/Turrets/";
    const string BossPath = TurretDir + "Turret_Boss.prefab";
    const string BossRockPath = "Assets/3D models/enemy_boss_rock.prefab";
    const string GreyRockPath = "Assets/3D models/enemy_rock.prefab";

    const string FloorMat = "Assets/materials/arena_floor.mat";
    const string WallMat = "Assets/materials/arena_wall.mat";
    const string RingMat = "Assets/materials/arena_ring.mat";

    // The box, agreed with Viktor 04.10.2026: 300 x 300 m, 80 m high, empty. Floor top at y = 0.
    const float Width = 300f;
    const float Depth = 300f;
    const float Height = 80f;
    const float Thickness = 2f;

    // Where the drone starts: near the south wall, facing north into the arena.
    // y 0.25: the drone's collider is ~0.39 m tall, so it rests on the floor rather than dropping in.
    static readonly Vector3 DroneSpawn = new Vector3(0f, 0.25f, -Depth * 0.5f + 30f);

    static readonly string[] Colours = { "Grey", "Blue", "Red", "Green" };

    [MenuItem("Tools/DroneMissile/Build Arena")]
    public static void BuildAll()
    {
        bool interactive = !Application.isBatchMode;
        if (interactive && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        GameObject grey = Load(TurretDir + "Turret_Grey.prefab");
        if (grey == null)
        {
            Debug.LogError("ArenaBuilder: turret prefabs missing - run Tools > DroneMissile > Build Turret Types first.");
            return;
        }

        Material floor = EnsureMaterial("Assets/materials/MAT_Platform.mat", FloorMat, new Color(0.32f, 0.33f, 0.36f), Color.black);
        Material wall = EnsureMaterial("Assets/materials/turret.mat", WallMat, new Color(0.18f, 0.19f, 0.22f), Color.black);
        Material ring = EnsureMaterial("Assets/materials/enemy laser.mat", RingMat, new Color(1f, 0.85f, 0.1f), new Color(4f, 3.2f, 0.3f));

        GameObject bossRock = EnsureBossRock();
        GameObject boss = EnsureBoss(bossRock);

        BuildScene(interactive, floor, wall, ring, boss);
        MainMenuBuilder.ConfigureBuildScenes();
    }

    static void BuildScene(bool interactive, Material floorMat, Material wallMat, Material ringMat, GameObject boss)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ArenaPath) != null)
        {
            if (interactive && !EditorUtility.DisplayDialog("Rebuild Arena",
                    ArenaPath + " already exists. Replace it?", "Replace", "Cancel"))
            {
                return;
            }
            AssetDatabase.DeleteAsset(ArenaPath);
        }
        if (!AssetDatabase.CopyAsset(SourceScene, ArenaPath))
        {
            Debug.LogError("ArenaBuilder: could not copy " + SourceScene + ".");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ArenaPath, OpenSceneMode.Single);

        // Out: the open-terrain map. Everything else (drone, camera, HUD, light, volume, Esc stopgap) stays.
        foreach (Terrain t in Object.FindObjectsByType<Terrain>()) Object.DestroyImmediate(t.gameObject);
        Destroy("turret");
        Destroy("Cube");

        // In: the box. Floor top at y = 0; walls and ceiling carry ArenaWall (stun-only on Easy, #20).
        GameObject box = new GameObject("ArenaBox");
        float hw = Width * 0.5f, hd = Depth * 0.5f, t2 = Thickness * 0.5f;
        Block(box, "Floor", new Vector3(0f, -t2, 0f), new Vector3(Width + 2f * Thickness, Thickness, Depth + 2f * Thickness), floorMat, false);
        Block(box, "Ceiling", new Vector3(0f, Height + t2, 0f), new Vector3(Width + 2f * Thickness, Thickness, Depth + 2f * Thickness), wallMat, true);
        Block(box, "Wall_N", new Vector3(0f, Height * 0.5f, hd + t2), new Vector3(Width + 2f * Thickness, Height, Thickness), wallMat, true);
        Block(box, "Wall_S", new Vector3(0f, Height * 0.5f, -hd - t2), new Vector3(Width + 2f * Thickness, Height, Thickness), wallMat, true);
        Block(box, "Wall_E", new Vector3(hw + t2, Height * 0.5f, 0f), new Vector3(Thickness, Height, Depth), wallMat, true);
        Block(box, "Wall_W", new Vector3(-hw - t2, Height * 0.5f, 0f), new Vector3(Thickness, Height, Depth), wallMat, true);

        GameObject drone = GameObject.FindGameObjectWithTag("Player");
        DroneControls controls = drone != null ? drone.GetComponent<DroneControls>() : null;
        if (controls != null)
        {
            drone.transform.SetPositionAndRotation(DroneSpawn, Quaternion.identity);
            controls.reloadSceneOnDeath = false;   // RunManager owns death here (#23)
            controls.killY = -20f;                 // the floor is at 0; nothing should get below it
        }
        else
        {
            Debug.LogWarning("ArenaBuilder: no Player-tagged drone in the copy.");
        }

        RunManager run = new GameObject("RunManager").AddComponent<RunManager>();
        run.greyTurret = Load(TurretDir + "Turret_Grey.prefab");
        run.blueTurret = Load(TurretDir + "Turret_Blue.prefab");
        run.redTurret = Load(TurretDir + "Turret_Red.prefab");
        run.greenTurret = Load(TurretDir + "Turret_Green.prefab");
        run.bossTurret = boss;
        run.ringMaterial = ringMat;
        run.floorCentre = Vector3.zero;
        run.floorSize = new Vector2(Width, Depth);
        run.height = Height;

        new GameObject("RunUI").AddComponent<RunUI>();

        EditorSceneManager.SaveScene(scene);
        Debug.Log("ArenaBuilder: built " + ArenaPath + ".");
    }

    // Writes RunManager.DefaultWaves() into Arena.unity without rebuilding it - the scene keeps its
    // own serialized copy of the table, which overrides the code default (docs/plans/waves-30.md).
    // Headless: Unity -batchmode -quit -executeMethod ArenaBuilder.ApplyWaveTable
    [MenuItem("Tools/DroneMissile/Apply Wave Table")]
    public static void ApplyWaveTable()
    {
        Scene scene = EditorSceneManager.OpenScene(ArenaPath, OpenSceneMode.Single);
        RunManager run = Object.FindAnyObjectByType<RunManager>();
        if (run == null)
        {
            Debug.LogError("ArenaBuilder: no RunManager in " + ArenaPath + " - run Build Arena first.");
            return;
        }

        Undo.RecordObject(run, "Apply Wave Table");
        run.waves = RunManager.DefaultWaves();
        EditorUtility.SetDirty(run);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("ArenaBuilder: " + run.waves.Length + " waves written to " + ArenaPath + ".");
    }

    // ---- assets ------------------------------------------------------------------------------

    // The boss's "big rock": the grey rock, three times the size, with a reliable sphere sweep and a
    // longer stun (agreed 04.10.2026). Unlike the grey turret's shot it does not tunnel.
    static GameObject EnsureBossRock()
    {
        GameObject existing = Load(BossRockPath);
        if (existing != null) return existing;

        AssetDatabase.CopyAsset(GreyRockPath, BossRockPath);
        GameObject root = PrefabUtility.LoadPrefabContents(BossRockPath);
        root.transform.localScale *= 3f;

        EnemyProjectile p = root.GetComponent<EnemyProjectile>();
        p.speed = 150f;
        p.lifeTime = 5f;
        p.stunDuration = 2f;
        p.stunKind = DroneControls.StunKind.Rock;
        p.useSweep = true;
        p.sweepRadius = 1.5f;

        PrefabUtility.SaveAsPrefabAsset(root, BossRockPath);
        PrefabUtility.UnloadPrefabContents(root);
        return Load(BossRockPath);
    }

    static GameObject EnsureBoss(GameObject bossRock)
    {
        GameObject existing = Load(BossPath);
        if (existing != null) return existing;

        AssetDatabase.CopyAsset(TurretDir + "Turret_Grey.prefab", BossPath);
        GameObject root = PrefabUtility.LoadPrefabContents(BossPath);

        // Uniform, so no non-uniform-scale skew (docs/decisions.md #4).
        root.transform.localScale = Vector3.one * 3f;

        // Its health bar is on screen (RunUI), not above it.
        foreach (TurretHealthBar bar in root.GetComponents<TurretHealthBar>()) Object.DestroyImmediate(bar);

        EnemyTurret turret = root.GetComponent<EnemyTurret>();
        turret.maxHealth = 600;
        turret.fireRate = 1.2f;
        turret.projectilePrefab = bossRock;

        BossTurret b = root.AddComponent<BossTurret>();
        b.turretPrefabs = new GameObject[Colours.Length];
        b.ballMaterials = new Material[Colours.Length];
        for (int i = 0; i < Colours.Length; i++)
        {
            b.turretPrefabs[i] = Load(TurretDir + "Turret_" + Colours[i] + ".prefab");
            b.ballMaterials[i] = AssetDatabase.LoadAssetAtPath<Material>("Assets/materials/turret_" + Colours[i].ToLowerInvariant() + ".mat");
        }

        PrefabUtility.SaveAsPrefabAsset(root, BossPath);
        PrefabUtility.UnloadPrefabContents(root);
        return Load(BossPath);
    }

    static Material EnsureMaterial(string sourcePath, string path, Color baseColor, Color emission)
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        AssetDatabase.CopyAsset(sourcePath, path);
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        m.SetColor("_BaseColor", baseColor);
        m.SetColor("_Color", baseColor);
        m.SetColor("_EmissionColor", emission);
        if (emission.maxColorComponent > 0f) m.EnableKeyword("_EMISSION");
        else m.DisableKeyword("_EMISSION");
        EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();
        return m;
    }

    // ---- helpers -----------------------------------------------------------------------------

    static void Block(GameObject parent, string name, Vector3 pos, Vector3 size, Material mat, bool wall)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        if (wall)
        {
            go.AddComponent<ArenaWall>();
            // The sun comes from outside the box; a shadow-casting ceiling would put the whole floor
            // in shade.
            go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    static void Destroy(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go != null) Object.DestroyImmediate(go);
    }

    static GameObject Load(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);
}
