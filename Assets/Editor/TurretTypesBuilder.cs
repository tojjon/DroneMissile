using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the four turret types (docs/design/enemies.md, docs/decisions.md #22): their materials,
/// projectile prefabs and turret prefabs, then places one of each in the Sandbox scene and gives
/// the drone its rock stun effect. Prefab and scene YAML are never hand-edited in this repo
/// (CLAUDE.md), so all of it goes through the editor API.
///
/// Missing assets are created; EXISTING ONES ARE LEFT ALONE, so Inspector tuning on the prefabs
/// survives a rerun. Delete an asset to have it regenerated with the starting values below.
///
/// Headless: Unity -batchmode -quit -executeMethod TurretTypesBuilder.BuildAll
/// </summary>
public static class TurretTypesBuilder
{
    const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    const string SandboxPath = "Assets/Scenes/Sandbox.unity";
    const string SourceProjectile = "Assets/3D models/enemy_rocket.prefab";
    const string TurretMaterial = "Assets/materials/turret.mat";
    const string ProjectileMaterial = "Assets/materials/enemy laser.mat";
    const string TurretDir = "Assets/Prefabs/Turrets";

    // Sandbox layout: one turret per compass direction, this far from the pad. Detection range is
    // 360 m, so 600 m out keeps the pad out of every turret's reach and puts ~850 m between
    // neighbours - their ranges never overlap and they engage one at a time.
    const float SandboxRingRadius = 600f;

    enum Kind { Grey, Blue, Red, Green }

    struct Spec
    {
        public Kind kind;
        public string projectile;     // prefab name in Assets/3D models/
        public Color body;            // turret colour
        public Color shot;            // projectile base colour
        public Color shotGlow;        // projectile emission (HDR)
        public float speed, lifeTime, fireRate, stun;
        public Vector3 sandboxDir;
    }

    // Starting values - all tunable on the prefabs afterwards. Grey is today's turret unchanged.
    static readonly Spec[] Specs =
    {
        new Spec { kind = Kind.Grey,  projectile = "enemy_rock",
                   body = new Color(0.45f, 0.45f, 0.47f), shot = new Color(0.55f, 0.5f, 0.45f), shotGlow = Color.black,
                   speed = 1080f, lifeTime = 5f, fireRate = 0.2f, stun = 1f, sandboxDir = Vector3.forward },
        new Spec { kind = Kind.Blue,  projectile = "enemy_electric",
                   body = new Color(0.15f, 0.35f, 1f), shot = new Color(0.4f, 0.7f, 1f), shotGlow = new Color(0.8f, 1.6f, 4f),
                   speed = 70f, lifeTime = 6f, fireRate = 1.5f, stun = 1f, sandboxDir = Vector3.right },
        new Spec { kind = Kind.Red,   projectile = "enemy_explosive",
                   body = new Color(0.9f, 0.12f, 0.1f), shot = new Color(1f, 0.3f, 0.1f), shotGlow = new Color(4f, 0.6f, 0.15f),
                   speed = 50f, lifeTime = 8f, fireRate = 2.5f, stun = 1f, sandboxDir = Vector3.back },
        new Spec { kind = Kind.Green, projectile = "enemy_homing",
                   body = new Color(0.15f, 0.75f, 0.2f), shot = new Color(0.3f, 1f, 0.4f), shotGlow = new Color(0.4f, 4f, 0.8f),
                   speed = 40f, lifeTime = 7f, fireRate = 3f, stun = 1f, sandboxDir = Vector3.left },
    };

    [MenuItem("Tools/DroneMissile/Build Turret Types")]
    public static void BuildAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Scene sample = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
        GameObject sourceTurret = GameObject.Find("turret");
        if (sourceTurret == null || sourceTurret.GetComponent<EnemyTurret>() == null)
        {
            Debug.LogError("TurretTypesBuilder: SampleScene has no 'turret' with EnemyTurret to copy.");
            return;
        }

        foreach (Spec s in Specs)
        {
            Material body = EnsureMaterial(TurretMaterial, "Assets/materials/turret_" + Lower(s.kind) + ".mat", s.body, Color.black);
            Material shot = EnsureMaterial(ProjectileMaterial, "Assets/materials/proj_" + Lower(s.kind) + ".mat", s.shot, s.shotGlow);
            GameObject projectile = EnsureProjectile(s, shot);
            EnsureTurret(s, sourceTurret, body, projectile);
        }

        // The rock look goes on the gameplay drone as well, or grey hits there would show only the
        // HUD border. Saved after the temporary turret copies are gone, so they never reach disk.
        AddRockStunEffect();
        EditorSceneManager.SaveScene(sample);

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SandboxPath) == null)
        {
            Debug.LogWarning("TurretTypesBuilder: no Sandbox scene yet - run Build All Menu Scenes; it places the turrets itself.");
            return;
        }

        Scene sandbox = EditorSceneManager.OpenScene(SandboxPath, OpenSceneMode.Single);
        PlaceInOpenSandbox();
        EditorSceneManager.SaveScene(sandbox);

        Debug.Log("TurretTypesBuilder: done.");
    }

    // Called by MainMenuBuilder after it re-copies the sandbox, so a fresh copy keeps its turrets.
    public static void PlaceInOpenSandbox()
    {
        // The copied original turret sits ~40 m from the pad and would open fire on spawn.
        GameObject original = GameObject.Find("turret");
        if (original != null) Object.DestroyImmediate(original);

        GameObject pad = GameObject.Find("Cube");
        Vector3 centre = pad != null ? pad.transform.position : Vector3.zero;

        foreach (Spec s in Specs)
        {
            string name = "Turret_" + s.kind;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TurretDir + "/" + name + ".prefab");
            if (prefab == null)
            {
                Debug.LogWarning("TurretTypesBuilder: " + name + " prefab missing - run Build Turret Types first.");
                continue;
            }

            GameObject old = GameObject.Find(name);
            if (old != null) Object.DestroyImmediate(old);

            GameObject t = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            t.name = name;
            Vector3 pos = centre + s.sandboxDir * SandboxRingRadius;
            t.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(-s.sandboxDir));
            SnapToGround(t);
        }

        AddRockStunEffect();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    // ---- assets ------------------------------------------------------------------------------

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

    static GameObject EnsureProjectile(Spec s, Material mat)
    {
        string path = "Assets/3D models/" + s.projectile + ".prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        AssetDatabase.CopyAsset(SourceProjectile, path);
        GameObject root = PrefabUtility.LoadPrefabContents(path);

        // Swap the copied base component for the type's own. Owner is left unset - the turret
        // assigns it on every shot.
        foreach (EnemyProjectile old in root.GetComponents<EnemyProjectile>()) Object.DestroyImmediate(old);

        EnemyProjectile p;
        switch (s.kind)
        {
            case Kind.Blue:
                p = root.AddComponent<ElectricProjectile>();
                p.stunKind = DroneControls.StunKind.Electric;
                p.useSweep = true;
                break;
            case Kind.Red:
                p = root.AddComponent<ExplosiveProjectile>();
                p.useSweep = true;
                break;
            case Kind.Green:
                p = root.AddComponent<HomingProjectile>();
                p.useSweep = true;
                break;
            default:
                // Grey: today's tunnelling trigger rocket (#14), rock look. visualPrefab stays empty
                // until Viktor's rock model exists.
                p = root.AddComponent<EnemyProjectile>();
                break;
        }
        p.speed = s.speed;
        p.lifeTime = s.lifeTime;
        p.stunDuration = s.stun;

        MeshRenderer r = root.GetComponent<MeshRenderer>();
        if (r != null) r.sharedMaterial = mat;

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    static void EnsureTurret(Spec s, GameObject source, Material body, GameObject projectile)
    {
        string path = TurretDir + "/Turret_" + s.kind + ".prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(TurretDir)) AssetDatabase.CreateFolder("Assets/Prefabs", "Turrets");

        // From a temporary copy, so SampleScene's own turret is not turned into a prefab instance.
        GameObject copy = Object.Instantiate(source);
        copy.name = "Turret_" + s.kind;
        copy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        EnemyTurret turret = copy.GetComponent<EnemyTurret>();
        turret.projectilePrefab = projectile;
        turret.fireRate = s.fireRate;

        Material original = AssetDatabase.LoadAssetAtPath<Material>(TurretMaterial);
        foreach (MeshRenderer r in copy.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (r.sharedMaterial == original) r.sharedMaterial = body;
        }

        PrefabUtility.SaveAsPrefabAsset(copy, path);
        Object.DestroyImmediate(copy);
    }

    // ---- scene helpers -----------------------------------------------------------------------

    // Rests the turret's lowest renderer on the terrain under it.
    static void SnapToGround(GameObject t)
    {
        Vector3 p = t.transform.position;
        Terrain terrain = TerrainAt(p);
        if (terrain == null)
        {
            Debug.LogWarning("TurretTypesBuilder: no terrain under " + t.name + "; left at " + p + ".");
            return;
        }

        float ground = terrain.transform.position.y + terrain.SampleHeight(p);
        t.transform.position = new Vector3(p.x, ground, p.z);

        Renderer[] rs = t.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return;
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        t.transform.position += Vector3.up * (ground - b.min.y);
    }

    static Terrain TerrainAt(Vector3 p)
    {
        foreach (Terrain terrain in Object.FindObjectsByType<Terrain>())
        {
            Vector3 o = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            if (p.x >= o.x && p.x <= o.x + size.x && p.z >= o.z && p.z <= o.z + size.z) return terrain;
        }
        return null;
    }

    // Second DroneStunArcs instance, retuned from lightning to yellow debris: the grey turret's
    // "rock" stun. Yellow is Viktor's placeholder (docs/design/enemies.md).
    static void AddRockStunEffect()
    {
        GameObject drone = GameObject.FindGameObjectWithTag("Player");
        if (drone == null || drone.GetComponent<DroneControls>() == null) return;

        foreach (DroneStunArcs a in drone.GetComponents<DroneStunArcs>())
        {
            if (a.kind == DroneControls.StunKind.Rock) return;
        }

        DroneStunArcs rock = Undo.AddComponent<DroneStunArcs>(drone);
        rock.kind = DroneControls.StunKind.Rock;
        rock.arcColor = new Color(4f, 3.2f, 0.4f, 1f);
        rock.arcRate = 45f;
        rock.onsetBurst = 35;
        rock.arcLifeMin = 0.5f;
        rock.arcLifeMax = 1.0f;
        rock.sizeMin = 0.08f;
        rock.sizeMax = 0.22f;
        rock.gravity = 0.6f;
        rock.noiseStrength = 0f;
        rock.useTrails = false;
        EditorSceneManager.MarkSceneDirty(drone.scene);
    }

    static string Lower(Kind k) => k.ToString().ToLowerInvariant();
}
