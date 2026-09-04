using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Drops the HUD object into the open scene. DroneHUD builds its own canvas at runtime, so all the
/// scene needs is one empty GameObject carrying the component - but scenes are not hand-edited as
/// YAML in this repo (see CLAUDE.md), so it goes in through the editor API like the turret body.
/// Idempotent - running it twice changes nothing.
/// </summary>
public static class HudBuilder
{
    [MenuItem("Tools/DroneMissile/Build HUD")]
    public static void Build()
    {
        GameObject hud = GameObject.Find("HUD");
        if (hud == null)
        {
            hud = new GameObject("HUD");
            Undo.RegisterCreatedObjectUndo(hud, "Create HUD");
        }

        if (hud.GetComponent<DroneHUD>() == null)
        {
            Undo.AddComponent<DroneHUD>(hud);
        }

        // DroneHUD finds the drone by tag and disables itself if it cannot, which is easy to miss
        // in a console full of the device dump from DroneControls.Start().
        if (GameObject.FindGameObjectWithTag("Player") == null)
        {
            Debug.LogWarning("HudBuilder: nothing in this scene is tagged 'Player'; DroneHUD will disable itself on play.");
        }

        EditorSceneManager.MarkSceneDirty(hud.scene);
        Debug.Log("HudBuilder: HUD ready. Save the scene (Ctrl+S) to keep it.");
    }
}
