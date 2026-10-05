using UnityEditor;
using UnityEngine;

/// <summary>
/// Puts Viktor's rock model on the grey turret's shots. Wraps Assets/Prefabs/Ammo/Rock.fbx in
/// RockVisual.prefab - re-centred on the mesh bounds and scaled to RockDiameter metres, with any
/// collider, camera or light from the export stripped - and assigns it to the `visualPrefab` slot
/// of enemy_rock (visualScale 1) and enemy_boss_rock (visualScale 3, matching its 1.5 m
/// sweepRadius). The visual is cosmetic only; hits still come from the projectile's own sweep.
///
/// Like TurretTypesBuilder it only fills what is missing: an existing RockVisual.prefab and an
/// already-set slot are left alone, so Inspector tuning survives a rerun. Delete the wrapper to
/// regenerate it.
/// Headless: Unity -batchmode -quit -executeMethod RockModelBuilder.Build
/// </summary>
public static class RockModelBuilder
{
    const string Model = "Assets/Prefabs/Ammo/Rock.fbx";
    const string Wrapper = "Assets/Prefabs/Ammo/RockVisual.prefab";
    const float RockDiameter = 1f;

    [MenuItem("Tools/DroneMissile/Apply Rock Model")]
    public static void Build()
    {
        GameObject visual = EnsureWrapper();
        if (visual == null) return;

        Assign("Assets/3D models/enemy_rock.prefab", visual, 1f);
        Assign("Assets/3D models/enemy_boss_rock.prefab", visual, 3f);
        AssetDatabase.SaveAssets();
    }

    static GameObject EnsureWrapper()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(Wrapper);
        if (existing != null) return existing;

        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(Model);
        if (model == null)
        {
            Debug.LogError("RockModelBuilder: " + Model + " not found.");
            return null;
        }

        GameObject root = new GameObject("RockVisual");
        GameObject mesh = (GameObject)PrefabUtility.InstantiatePrefab(model);
        mesh.transform.SetParent(root.transform, false);

        // Blender exports can carry a camera, a light or a collision mesh; none belong on a shot.
        foreach (Collider c in mesh.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        foreach (Camera c in mesh.GetComponentsInChildren<Camera>(true)) Object.DestroyImmediate(c.gameObject);
        foreach (Light l in mesh.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(l.gameObject);

        Renderer[] renderers = mesh.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            Debug.LogError("RockModelBuilder: " + Model + " has no renderer.");
            Object.DestroyImmediate(root);
            return null;
        }

        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        Debug.Log("RockModelBuilder: model bounds centre " + b.center + ", size " + b.size + " (as imported).");

        // Fit the largest dimension to RockDiameter and move the mesh so its centre sits on the
        // projectile's origin - the pivot in the export may be anywhere.
        float s = RockDiameter / Mathf.Max(b.size.x, b.size.y, b.size.z);
        // Scaling happens about the mesh's own pivot, which stays put; the bounds centre moves to
        // pivot + s * (centre - pivot), so shift by exactly that to land it on 0.
        Vector3 pivot = mesh.transform.localPosition;
        mesh.transform.localScale *= s;
        mesh.transform.localPosition = -s * (b.center - pivot);

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, Wrapper);
        Object.DestroyImmediate(root);
        return saved;
    }

    static void Assign(string path, GameObject visual, float scale)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        EnemyProjectile p = root.GetComponent<EnemyProjectile>();
        if (p == null)
        {
            Debug.LogWarning("RockModelBuilder: no EnemyProjectile on " + path + " - run Build Turret Types / Build Arena first.");
        }
        else if (p.visualPrefab == null)
        {
            p.visualPrefab = visual;
            p.visualScale = scale;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        PrefabUtility.UnloadPrefabContents(root);
    }
}
