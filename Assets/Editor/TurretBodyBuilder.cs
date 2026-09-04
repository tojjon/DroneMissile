using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-click scene surgery for the turret body. Scenes are not hand-edited as YAML in this repo
/// (see CLAUDE.md), so the geometry from docs/decisions.md #9 is applied through the editor API
/// instead. Idempotent - running it twice just re-applies the same values.
/// </summary>
public static class TurretBodyBuilder
{
    const string MaterialPath = "Assets/materials/turret.mat";

    [MenuItem("Tools/DroneMissile/Build Turret Body")]
    public static void Build()
    {
        GameObject turret = GameObject.Find("turret");
        if (turret == null)
        {
            Debug.LogError("TurretBodyBuilder: no GameObject named 'turret' in the open scene.");
            return;
        }

        Transform barrel = turret.transform.Find("Turret_Barrel");
        if (barrel == null)
        {
            Debug.LogError("TurretBodyBuilder: 'turret' has no child 'Turret_Barrel'.");
            return;
        }

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat == null) Debug.LogWarning("TurretBodyBuilder: " + MaterialPath + " not found; leaving default material.");

        // Body: fills the gap between the base slab (top at y 0.5) and the barrel pivot (y 1.93).
        // Children of `turret`, NOT of the barrel - the barrel rotates, the body must not.
        // Their colliders are kept on purpose: RocketProjectile walks GetComponentInParent<EnemyTurret>()
        // from whatever it hits, so this makes the visible body hittable.
        Upsert(turret.transform, "Turret_Mount", PrimitiveType.Cylinder,
               new Vector3(0f, 1.0f, 0f), new Vector3(2.2f, 0.75f, 2.2f), mat);

        Upsert(turret.transform, "Turret_Head", PrimitiveType.Sphere,
               new Vector3(0f, 1.93f, 0f), new Vector3(1.6f, 1.6f, 1.6f), mat);

        // Barrel visual: 3 m long, 1 m across in world units (parent scale is 2). Starts at the
        // pivot so it sinks into the head instead of floating in front of it.
        Transform mesh = barrel.Find("Turret_BarrelMesh");
        if (mesh != null)
        {
            Undo.RecordObject(mesh, "Retune barrel mesh");
            mesh.localPosition = new Vector3(0f, 0f, 0.75f);
            mesh.localRotation = Quaternion.Euler(90f, 0f, 0f); // capsule mesh is long along Y
            mesh.localScale = new Vector3(0.5f, 0.75f, 0.5f);
        }
        else Debug.LogWarning("TurretBodyBuilder: 'Turret_BarrelMesh' not found under Turret_Barrel.");

        // Muzzle. x and y MUST stay 0 - that is what keeps it on the aim axis (decision #9).
        Transform firePoint = barrel.Find("TurretFirePoint");
        if (firePoint != null)
        {
            Undo.RecordObject(firePoint, "Move muzzle to barrel tip");
            firePoint.localPosition = new Vector3(0f, 0f, 1.55f);
            firePoint.localRotation = Quaternion.identity; // Fire() relies on this
        }
        else Debug.LogWarning("TurretBodyBuilder: 'TurretFirePoint' not found under Turret_Barrel.");

        // Barrel hitbox, matched to the visual. Rotates with the barrel because it lives on it.
        CapsuleCollider col = barrel.GetComponent<CapsuleCollider>();
        if (col != null)
        {
            Undo.RecordObject(col, "Align barrel collider");
            col.direction = 2; // Z
            col.radius = 0.25f;
            col.height = 1.5f;
            col.center = new Vector3(0f, 0f, 0.75f);
        }
        else Debug.LogWarning("TurretBodyBuilder: Turret_Barrel has no CapsuleCollider.");

        EditorSceneManager.MarkSceneDirty(turret.scene);
        Debug.Log("TurretBodyBuilder: turret body built. Save the scene (Ctrl+S) to keep it.");
    }

    static void Upsert(Transform parent, string name, PrimitiveType type,
                       Vector3 localPos, Vector3 localScale, Material mat)
    {
        Transform t = parent.Find(name);
        if (t == null)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            t = go.transform;
        }
        else
        {
            Undo.RecordObject(t, "Update " + name);
        }

        t.localPosition = localPos;
        t.localRotation = Quaternion.identity;
        t.localScale = localScale;

        if (mat != null)
        {
            MeshRenderer r = t.GetComponent<MeshRenderer>();
            if (r != null)
            {
                Undo.RecordObject(r, "Assign material");
                r.sharedMaterial = mat;
            }
        }
    }
}
