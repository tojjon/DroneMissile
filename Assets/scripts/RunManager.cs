using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The wave run in the Arena (docs/design/game-structure.md, waves.md, docs/decisions.md #23).
/// Spawns each wave's turrets and yellow rings, notices when the wave is cleared, pauses for the
/// between-wave screen, and decides what death means: Easy restarts the wave in place, Normal ends
/// the run. Waves 10, 20 and 30 are the boss; clearing the last wave is victory.
///
/// Polls everything - the drone's HasCrashed, the turret list - like the rest of the project.
/// RunUI polls this in turn. The drone in the Arena has reloadSceneOnDeath off, so this is the only
/// thing that reacts to a crash here.
/// </summary>
public class RunManager : MonoBehaviour
{
    // Banner: Hardcore's 2 s "wave cleared" between waves - the game keeps running (#25).
    public enum State { Playing, Intermission, Banner, Ended }

    // Ball colours a boss wave allows. Same order as BossTurret.turretPrefabs. None (0) = all four,
    // so wave tables saved before this field existed keep wave 10 as it was (docs/plans/waves-30.md).
    [System.Flags]
    public enum BallColours { None = 0, Grey = 1, Blue = 2, Red = 4, Green = 8 }

    [System.Serializable]
    public class Wave
    {
        public int grey, blue, red, green;
        public bool boss;

        [Tooltip("Boss waves only: which colours the boss's balls may be. Nothing ticked = all four.")]
        public BallColours bossBalls;

        public Wave(int grey, int blue, int red, int green, bool boss = false, BallColours bossBalls = BallColours.None)
        {
            this.grey = grey; this.blue = blue; this.red = red; this.green = green; this.boss = boss;
            this.bossBalls = bossBalls;
        }
    }

    // The designed table (docs/design/waves.md). Static so Tools > DroneMissile > Apply Wave Table can
    // write it into Arena.unity - the scene keeps its own serialized copy, which beats this default.
    public static Wave[] DefaultWaves()
    {
        return new[]
        {
            new Wave(1, 0, 0, 0),
            new Wave(2, 0, 0, 0),
            new Wave(1, 1, 0, 0),
            new Wave(2, 1, 0, 0),
            new Wave(2, 0, 1, 0),
            new Wave(1, 1, 1, 0),
            new Wave(2, 0, 0, 1),
            new Wave(1, 1, 1, 1),
            new Wave(2, 2, 2, 2),
            new Wave(0, 0, 0, 0, boss: true),                                   // 10
            new Wave(3, 1, 1, 1),
            new Wave(2, 2, 1, 1),
            new Wave(2, 1, 2, 1),
            new Wave(2, 1, 1, 2),
            new Wave(3, 2, 2, 1),
            new Wave(2, 2, 2, 2),
            new Wave(3, 2, 2, 2),
            new Wave(2, 3, 3, 2),
            new Wave(3, 3, 3, 3),
            new Wave(0, 0, 0, 0, boss: true,                                    // 20: no grey balls
                     bossBalls: BallColours.Blue | BallColours.Red | BallColours.Green),
            new Wave(2, 2, 2, 2),
            new Wave(3, 2, 2, 2),
            new Wave(2, 3, 2, 3),
            new Wave(3, 3, 3, 2),
            new Wave(3, 3, 3, 3),
            new Wave(4, 3, 3, 3),
            new Wave(3, 4, 3, 4),
            new Wave(4, 4, 4, 3),
            new Wave(4, 4, 4, 4),
            new Wave(0, 0, 0, 0, boss: true, bossBalls: BallColours.Green),     // 30: green only
        };
    }

    [Header("Turrets")]
    public GameObject greyTurret;
    public GameObject blueTurret;
    public GameObject redTurret;
    public GameObject greenTurret;
    public GameObject bossTurret;

    [Header("Waves")]
    [Tooltip("One entry per wave. The last one is the final wave - clearing it wins the run.")]
    public Wave[] waves = DefaultWaves();

    [Header("Arena (set by Tools > DroneMissile > Build Arena)")]
    [Tooltip("Centre of the floor's top surface.")]
    public Vector3 floorCentre = Vector3.zero;
    public Vector2 floorSize = new Vector2(300f, 300f);
    public float height = 80f;

    [Header("Spawning")]
    [Tooltip("Keep turrets and rings this far from the walls.")]
    public float wallMargin = 20f;
    public float minTurretSpacing = 40f;
    [Tooltip("No turret spawns closer than this to the drone.")]
    public float minDistanceFromDrone = 60f;

    [Header("Hardcore")]
    [Tooltip("Seconds the WAVE CLEARED banner shows before the next wave starts on its own.")]
    public float hardcoreBannerTime = 2f;

    [Header("Yellow Rings")]
    public int ringsPerWave = 3;
    public Material ringMaterial;
    public float ringMinHeight = 10f;
    public float ringMaxHeight = 60f;

    public static RunManager Instance { get; private set; }

    public State CurrentState { get; private set; }
    public int CurrentWave { get; private set; }
    public int WaveCount => waves.Length;
    public bool Victory { get; private set; }
    public bool NewRecord { get; private set; }
    public EnemyTurret Boss { get; private set; }

    private readonly List<EnemyTurret> live = new List<EnemyTurret>();
    private DroneControls drone;
    private Vector3 spawnPos;
    private Quaternion spawnRot;
    private float nextWaveAt;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        Time.timeScale = 1f;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) drone = player.GetComponent<DroneControls>();
        if (drone == null)
        {
            Debug.LogError("RunManager: no DroneControls on a Player-tagged object. The run cannot start.");
            enabled = false;
            return;
        }

        // Belt and braces - the builder already turns it off. A reload here would wipe the run.
        drone.reloadSceneOnDeath = false;
        spawnPos = drone.transform.position;
        spawnRot = drone.transform.rotation;

        GameSession.ResetRun();
        StartWave(1);
    }

    void Update()
    {
        if (CurrentState != State.Playing && CurrentState != State.Banner) return;

        // Death counts during the banner too - the world keeps running on Hardcore.
        if (drone.DeathDelayElapsed)
        {
            OnDeath();
            return;
        }

        if (CurrentState == State.Banner)
        {
            if (Time.time >= nextWaveAt) StartWave(CurrentWave + 1);
            return;
        }

        live.RemoveAll(t => t == null);
        if (live.Count == 0 && ColorBall.InFlight == 0) OnWaveCleared();
    }

    // Boss balls register the turrets they spawn, so the wave waits for them too.
    public void Register(EnemyTurret turret)
    {
        if (turret != null && CurrentState == State.Playing) live.Add(turret);
    }

    // Called by RunUI's Continue button on the between-wave screen.
    public void ContinueToNextWave()
    {
        if (CurrentState != State.Intermission) return;
        StartWave(CurrentWave + 1);
    }

    // ---- flow ---------------------------------------------------------------------------------

    void StartWave(int n)
    {
        CurrentWave = n;
        GameSession.Wave = n;
        GameSession.ResetWaveStats();
        Time.timeScale = 1f;

        ClearWorld();

        Wave w = waves[n - 1];
        Boss = null;
        if (w.boss && bossTurret != null)
        {
            Boss = SpawnTurret(bossTurret, farFromDrone: true);
            BossTurret bt = Boss != null ? Boss.GetComponent<BossTurret>() : null;
            if (bt != null) bt.ballColours = w.bossBalls;
        }
        for (int i = 0; i < w.grey; i++) SpawnTurret(greyTurret);
        for (int i = 0; i < w.blue; i++) SpawnTurret(blueTurret);
        for (int i = 0; i < w.red; i++) SpawnTurret(redTurret);
        for (int i = 0; i < w.green; i++) SpawnTurret(greenTurret);

        for (int i = 0; i < ringsPerWave; i++) SpawnRing();

        if (live.Count == 0)
        {
            Debug.LogWarning("RunManager: wave " + n + " spawned no turrets - check the prefab slots.");
        }

        CurrentState = State.Playing;
    }

    void OnWaveCleared()
    {
        if (CurrentWave >= waves.Length)
        {
            End(true);
            return;
        }

        if (GameSession.CurrentDifficulty == GameSession.Difficulty.Hardcore)
        {
            // No cards on Hardcore, so no pause either: a short banner, then the next wave.
            CurrentState = State.Banner;
            nextWaveAt = Time.time + hardcoreBannerTime;
            return;
        }

        CurrentState = State.Intermission;
        Time.timeScale = 0f;   // the between-wave screen; Shoting and the physics step both stop
    }

    void OnDeath()
    {
        if (GameSession.CurrentDifficulty == GameSession.Difficulty.Easy)
        {
            // Same wave from the start: the drone back on its spawn, turrets and rings respawned.
            drone.ResetTo(spawnPos, spawnRot);
            StartWave(CurrentWave);
        }
        else
        {
            End(false);
        }
    }

    void End(bool victory)
    {
        Victory = victory;
        NewRecord = GameSession.SubmitBestWave(CurrentWave);
        CurrentState = State.Ended;
        Time.timeScale = 0f;
    }

    // Everything a wave leaves behind: its turrets and anything still flying.
    void ClearWorld()
    {
        foreach (EnemyTurret t in live) if (t != null) Destroy(t.gameObject);
        live.Clear();

        foreach (EnemyProjectile p in FindObjectsByType<EnemyProjectile>()) Destroy(p.gameObject);
        foreach (RocketProjectile p in FindObjectsByType<RocketProjectile>()) Destroy(p.gameObject);
        foreach (ColorBall b in FindObjectsByType<ColorBall>()) Destroy(b.gameObject);
        foreach (YellowRing r in FindObjectsByType<YellowRing>()) Destroy(r.gameObject);
    }

    // ---- spawning -----------------------------------------------------------------------------

    EnemyTurret SpawnTurret(GameObject prefab, bool farFromDrone = false)
    {
        if (prefab == null) return null;

        Vector3 pos = farFromDrone ? FarthestFloorPoint() : FreeFloorPoint();
        GameObject go = Instantiate(prefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        RestOnGround(go, floorCentre.y);

        EnemyTurret turret = go.GetComponent<EnemyTurret>();
        if (turret != null) live.Add(turret);
        return turret;
    }

    // A random floor point clear of the walls, the other turrets and the drone. Gives up after a few
    // tries and takes the last candidate - a crowded wave is better than a missing turret.
    Vector3 FreeFloorPoint()
    {
        Vector3 candidate = floorCentre;
        for (int attempt = 0; attempt < 40; attempt++)
        {
            candidate = RandomFloorPoint();
            if (Clear(candidate)) break;
        }
        return candidate;
    }

    Vector3 FarthestFloorPoint()
    {
        Vector3 best = floorCentre;
        float bestDist = -1f;
        for (int i = 0; i < 12; i++)
        {
            Vector3 c = RandomFloorPoint();
            float d = FlatDistance(c, drone.transform.position);
            if (d > bestDist) { bestDist = d; best = c; }
        }
        return best;
    }

    bool Clear(Vector3 p)
    {
        if (FlatDistance(p, drone.transform.position) < minDistanceFromDrone) return false;
        foreach (EnemyTurret t in live)
        {
            if (t != null && FlatDistance(p, t.transform.position) < minTurretSpacing) return false;
        }
        return true;
    }

    Vector3 RandomFloorPoint()
    {
        float hx = Mathf.Max(0f, floorSize.x * 0.5f - wallMargin);
        float hz = Mathf.Max(0f, floorSize.y * 0.5f - wallMargin);
        return floorCentre + new Vector3(Random.Range(-hx, hx), 0f, Random.Range(-hz, hz));
    }

    void SpawnRing()
    {
        Vector3 p = RandomFloorPoint();
        p.y = floorCentre.y + Random.Range(ringMinHeight, Mathf.Min(ringMaxHeight, height - 5f));

        GameObject go = new GameObject("YellowRing");
        go.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        YellowRing ring = go.AddComponent<YellowRing>();
        ring.material = ringMaterial;
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f; b.y = 0f;
        return Vector3.Distance(a, b);
    }

    // Lifts an object so its lowest renderer sits at groundY. Turret prefabs have their root at the
    // old scene position's height, not at their base, so the root cannot simply be put on the floor.
    public static void RestOnGround(GameObject go, float groundY)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return;

        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        go.transform.position += Vector3.up * (groundY - b.min.y);
    }
}
