using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Puts DroneMotorSound (and the AudioSource it requires) on the Player-tagged drone in every scene
/// the drone flies in (docs/plans/motor-sound.md). Scenes are never hand-edited as YAML (CLAUDE.md),
/// so it goes in through the editor API like the HUD and the stun arcs. Idempotent.
///
/// Headless: Unity -batchmode -quit -executeMethod DroneMotorSoundBuilder.BuildAll (or .ResetAll)
/// The menu scene is deliberately not listed - its drone has no DroneControls (MainMenuBuilder).
/// </summary>
public static class DroneMotorSoundBuilder
{
    static readonly string[] Scenes =
    {
        "Assets/Scenes/SampleScene.unity",
        "Assets/Scenes/Sandbox.unity",
        "Assets/Scenes/Arena.unity",
    };

    [MenuItem("Tools/DroneMissile/Build Drone Motor Sound")]
    public static void BuildAll() => Apply(false);

    // Replaces the component in every scene, so its settings go back to the script defaults - the
    // scenes keep their own serialized copy, which beats a changed default. Inspector tuning is lost.
    [MenuItem("Tools/DroneMissile/Reset Drone Motor Sound")]
    public static void ResetAll() => Apply(true);

    static void Apply(bool reset)
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        foreach (string path in Scenes)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                Debug.LogWarning("DroneMotorSoundBuilder: " + path + " does not exist - skipped.");
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            GameObject drone = GameObject.FindGameObjectWithTag("Player");
            if (drone == null || drone.GetComponent<DroneControls>() == null)
            {
                Debug.LogWarning("DroneMotorSoundBuilder: no Player-tagged drone with DroneControls in " + path + ".");
                continue;
            }

            DroneMotorSound existing = drone.GetComponent<DroneMotorSound>();
            if (existing != null && reset) Object.DestroyImmediate(existing);
            else if (existing != null)
            {
                Debug.Log("DroneMotorSoundBuilder: " + path + " already has the motor sound.");
                continue;
            }

            drone.AddComponent<DroneMotorSound>();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("DroneMotorSoundBuilder: motor sound added to " + drone.name + " in " + path + ".");
        }
    }
}
