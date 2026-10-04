using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sets the FPV camera uptilt on the drone - and the muzzle with it. Main Camera and FirePoint are
/// siblings under the drone and MUST keep bit-identical local rotations: that is what makes the
/// static screen-centre crosshair the exact impact point (docs/decisions.md #12). Never tilt one
/// without the other.
///
/// Scenes are not hand-edited as YAML (CLAUDE.md), so this goes through the editor API.
/// Headless: Unity -batchmode -quit -executeMethod CameraTiltBuilder.BuildAll
/// </summary>
public static class CameraTiltBuilder
{
    // Degrees the camera looks up from the drone's forward axis. Viktor's choice, 04.10.2026.
    public const float UptiltDegrees = 35f;

    static readonly string[] Scenes =
    {
        "Assets/Scenes/SampleScene.unity",
        "Assets/Scenes/Sandbox.unity",
        "Assets/Scenes/Arena.unity",
    };

    [MenuItem("Tools/DroneMissile/Apply Camera Uptilt (open scene)")]
    public static void ApplyToOpenScene()
    {
        if (Apply()) EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    [MenuItem("Tools/DroneMissile/Apply Camera Uptilt (all gameplay scenes)")]
    public static void BuildAll()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        foreach (string path in Scenes)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (Apply()) EditorSceneManager.SaveScene(scene);
        }
    }

    static bool Apply()
    {
        GameObject drone = GameObject.FindGameObjectWithTag("Player");
        if (drone == null)
        {
            Debug.LogWarning("CameraTiltBuilder: no Player-tagged drone in " + SceneManager.GetActiveScene().name + ".");
            return false;
        }

        Transform cam = drone.transform.Find("Main Camera");
        Transform muzzle = drone.transform.Find("FirePoint");
        if (cam == null || muzzle == null)
        {
            Debug.LogError("CameraTiltBuilder: drone needs both 'Main Camera' and 'FirePoint' children.");
            return false;
        }

        // Negative X is nose-up in Unity. One quaternion for both, so they stay bit-identical.
        Quaternion tilt = Quaternion.Euler(-UptiltDegrees, 0f, 0f);
        Undo.RecordObjects(new Object[] { cam, muzzle }, "Camera uptilt");
        cam.localRotation = tilt;
        muzzle.localRotation = tilt;

        Debug.Log("CameraTiltBuilder: " + SceneManager.GetActiveScene().name + " camera + FirePoint at " + UptiltDegrees + " deg uptilt.");
        return true;
    }
}
