using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class Shoting : MonoBehaviour
{
    public GameObject rocketPrefab;
    public Transform firePoint;
    public float fireRate = 0.5f; // seconds between shots

    // Upgrade tuning lives here, next to the gun that uses it (docs/plans/upgrades.md). How many
    // of each the player has comes from GameSession.Stacks.
    [Header("Upgrade: Double Strike")]
    [Tooltip("Seconds between rockets in one burst.")]
    public float burstInterval = 0.1f;

    [Header("Upgrade: Homing")]
    [Tooltip("Side of the square scope as a fraction of screen height, with one Homing card.")]
    public float homingScopeBase = 0.25f;
    [Tooltip("Added to the scope's side per extra card.")]
    public float homingScopePerStack = 0.1f;
    [Range(0.1f, 1f)]
    public float homingScopeMax = 0.8f;
    [Tooltip("Degrees per second a homing rocket turns, with one card.")]
    public float homingTurnBase = 90f;
    public float homingTurnPerStack = 60f;
    [Tooltip("Turrets further than this are never locked.")]
    public float homingRange = 300f;

    [Header("Upgrade: Lightning")]
    [Tooltip("How far a zap jumps from one turret to the next, in metres.")]
    public float chainRange = 160f;
    [Range(0f, 1f)]
    [Tooltip("Share of the rocket's damage each jump deals.")]
    public float chainDamageFraction = 0.5f;

    private Joystick transmitter;
    private float nextFireTime = 0f;

    // Ring bonus for the next shot, stacking: each yellow ring adds 1 (docs/design/waves.md).
    private int pendingBonus;
    public int PendingBonus => pendingBonus;

    // Double Strike burst in progress. A timer rather than a coroutine, so the pause screens
    // (timeScale 0) freeze it like everything else.
    private int burstRemaining;
    private int burstBonus;
    private float nextBurstShot;

    // Read by DroneHUD to draw and colour the scope.
    public float HomingScopeFraction { get; private set; }
    public bool HomingLocked { get; private set; }

    public void AddBonus(int amount)
    {
        pendingBonus += amount;
    }

    void Start()
    {
        // Reuse the same device-finding logic as DroneControls
        foreach (var joystick in Joystick.all)
        {
            if (joystick.displayName.Contains("Joystick1") || joystick.name.Contains("Joystick1"))
            {
                transmitter = joystick;
                break;
            }
        }

        if (transmitter == null && Joystick.all.Count > 0)
        {
            transmitter = Joystick.all[Joystick.all.Count - 1];
        }
    }

    void Update()
    {
        // Paused (between-wave screen, end screen): Time.time stands still, so the cooldown would
        // let exactly one rocket out into the frozen world.
        if (Time.timeScale == 0f) return;

        UpdateHomingScope();

        // Rockets still owed from the current Double Strike burst.
        while (burstRemaining > 0 && Time.time >= nextBurstShot)
        {
            FireOne(burstBonus);
            burstRemaining--;
            nextBurstShot += burstInterval;
        }

        bool triggerPressed = false;

        // Transmitter trigger
        if (transmitter != null)
        {
            var trigger = transmitter.TryGetChildControl<ButtonControl>("trigger");
            if (trigger != null) triggerPressed = trigger.isPressed;
        }

        // Keyboard fallback (Spacebar)
        if (Keyboard.current.spaceKey.isPressed) triggerPressed = true;

        // A new burst only once the last one is out and the cooldown has passed.
        if (triggerPressed && burstRemaining == 0 && Time.time >= nextFireTime)
        {
            // One press = 1 rocket, +1 per Double Strike card. The ring bonus rides on EVERY rocket
            // of the burst (Viktor, 04.10.2026) and is spent by the burst as a whole.
            burstRemaining = 1 + GameSession.Stacks(UpgradeIds.DoubleStrike);
            burstBonus = pendingBonus;
            pendingBonus = 0;
            nextBurstShot = Time.time;
            nextFireTime = Time.time + fireRate;

            FireOne(burstBonus);
            burstRemaining--;
            nextBurstShot += burstInterval;
        }
    }

    void FireOne(int bonus)
    {
        if (rocketPrefab == null || firePoint == null)
        {
            Debug.LogWarning("Rocket prefab or FirePoint not assigned!");
            return;
        }

        GameObject rocket = Instantiate(rocketPrefab, firePoint.position, firePoint.rotation);

        // Tell the rocket who fired it so it can filter out self-hits. This script sits on the
        // drone root, which is exactly the hierarchy the rocket must ignore - it spawns partly
        // inside the drone's own collider.
        RocketProjectile proj = rocket.GetComponent<RocketProjectile>();
        if (proj == null) return;

        proj.owner = transform;
        proj.damage += bonus;   // boosted rockets blink

        int homing = GameSession.Stacks(UpgradeIds.Homing);
        if (homing > 0)
        {
            proj.homingTarget = FindScopeTarget();
            proj.homingTurnRate = homingTurnBase + homingTurnPerStack * (homing - 1);
        }

        int chain = GameSession.Stacks(UpgradeIds.Lightning);
        if (chain > 0)
        {
            proj.chainJumps = chain;
            proj.chainRange = chainRange;
            proj.chainDamageFraction = chainDamageFraction;
        }
    }

    // ---- Homing ------------------------------------------------------------------------------

    void UpdateHomingScope()
    {
        int homing = GameSession.Stacks(UpgradeIds.Homing);
        HomingScopeFraction = homing > 0
            ? Mathf.Min(homingScopeMax, homingScopeBase + homingScopePerStack * (homing - 1))
            : 0f;
        HomingLocked = homing > 0 && FindScopeTarget() != null;
    }

    // The turret whose aim point projects inside the square scope, closest to the screen centre -
    // which is also the crosshair and the unguided impact point (docs/decisions.md #12).
    EnemyTurret FindScopeTarget()
    {
        Camera cam = Camera.main;
        if (cam == null || HomingScopeFraction <= 0f) return null;

        float half = HomingScopeFraction * Screen.height * 0.5f;
        Vector2 centre = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        EnemyTurret best = null;
        float bestDist = float.PositiveInfinity;
        foreach (EnemyTurret t in FindObjectsByType<EnemyTurret>())
        {
            if (!t.Alive) continue;
            Vector3 aim = t.AimPoint;
            if ((aim - transform.position).sqrMagnitude > homingRange * homingRange) continue;

            Vector3 sp = cam.WorldToScreenPoint(aim);
            if (sp.z <= 0f) continue;   // behind the camera

            Vector2 d = (Vector2)sp - centre;
            if (Mathf.Abs(d.x) > half || Mathf.Abs(d.y) > half) continue;

            if (d.sqrMagnitude < bestDist)
            {
                bestDist = d.sqrMagnitude;
                best = t;
            }
        }
        return best;
    }
}
