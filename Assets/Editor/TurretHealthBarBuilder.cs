using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Adds TurretHealthBar to every turret in the open scene. The bar builds its own canvas at runtime,
/// so all the scene needs is the component on the same object as EnemyTurret - but scenes are not
/// hand-edited as YAML in this repo (see CLAUDE.md), so it goes in through the editor API like the
/// HUD and the turret body. Idempotent - running it twice changes nothing.
/// </summary>
public static class TurretHealthBarBuilder
{
    [MenuItem("Tools/DroneMissile/Build Turret Health Bars")]
    public static void Build()
    {
        // Include inactive: a turret parked disabled in the scene still wants the component, and it
        // would silently be skipped otherwise.
        EnemyTurret[] turrets = Object.FindObjectsByType<EnemyTurret>(FindObjectsInactive.Include);
        if (turrets.Length == 0)
        {
            Debug.LogError("TurretHealthBarBuilder: no EnemyTurret in the open scene.");
            return;
        }

        int added = 0;
        foreach (EnemyTurret turret in turrets)
        {
            if (turret.GetComponent<TurretHealthBar>() != null) continue;

            Undo.AddComponent<TurretHealthBar>(turret.gameObject);
            added++;
        }

        if (added == 0)
        {
            Debug.Log("TurretHealthBarBuilder: all " + turrets.Length + " turret(s) already have a health bar.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(turrets[0].gameObject.scene);
        Debug.Log("TurretHealthBarBuilder: health bar added to " + added + " of " + turrets.Length
                  + " turret(s). Save the scene (Ctrl+S) to keep it.");
    }
}
