using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Adds DroneStunArcs to the drone. The component builds its own rig at runtime, so all the scene
/// needs is the component on the Player-tagged root - but scenes are not hand-edited as YAML in this
/// repo (see CLAUDE.md), so it goes in through the editor API like the HUD and the turret health
/// bars. Idempotent - running it twice changes nothing.
/// </summary>
public static class DroneStunArcsBuilder
{
    [MenuItem("Tools/DroneMissile/Build Drone Stun Arcs")]
    public static void Build()
    {
        // Tag contract, same as DroneHUD and EnemyTurret targeting: the drone root carries Player.
        GameObject drone = GameObject.FindGameObjectWithTag("Player");
        if (drone == null)
        {
            Debug.LogError("DroneStunArcsBuilder: nothing in the open scene is tagged 'Player'.");
            return;
        }

        // DroneStunArcs has [RequireComponent(typeof(DroneControls))], so AddComponent would drag one
        // in rather than fail. Checking first turns that silent surprise into a readable error.
        if (drone.GetComponent<DroneControls>() == null)
        {
            Debug.LogError("DroneStunArcsBuilder: " + drone.name + " is tagged Player but has no "
                           + "DroneControls; the arcs would have no IsStunned to poll.");
            return;
        }

        if (drone.GetComponent<DroneStunArcs>() != null)
        {
            Debug.Log("DroneStunArcsBuilder: " + drone.name + " already has stun arcs.");
            return;
        }

        Undo.AddComponent<DroneStunArcs>(drone);

        EditorSceneManager.MarkSceneDirty(drone.scene);
        Debug.Log("DroneStunArcsBuilder: stun arcs added to " + drone.name
                  + ". Save the scene (Ctrl+S) to keep it.");
    }
}
