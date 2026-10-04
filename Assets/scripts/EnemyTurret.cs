using UnityEngine;

public class EnemyTurret : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform barrel;
    public Transform firePoint;

    public float detectionRange = 30f;
    public float fireRate = 2f;
    public float turnSpeed = 90f;

    [Header("Health")]
    public int maxHealth = 30;
    private int currentHealth;

    private Transform player;
    private float nextFireTime = 0f;

    // Read-only state for TurretHealthBar, which polls it once a frame - the same arrangement as
    // DroneControls.IsStunned / .HasCrashed. currentHealth stays private and TakeDamage() remains
    // the only thing that changes it. See docs/decisions.md #13.
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public float HealthFraction => maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / maxHealth) : 0f;

    // What player upgrades aim at - Homing steers here, Lightning zaps here. The barrel pivot is the
    // head of the turret; the root sits at its base (or, in prefabs, wherever the old scene had it).
    public Vector3 AimPoint => barrel != null ? barrel.position : transform.position;

    // False from the hit that kills it until Destroy lands at the end of the frame.
    public bool Alive => currentHealth > 0;

    // Awake, not Start: TurretHealthBar reads HealthFraction in its own Start(), and Start() order
    // between two components on the same object is undefined. Awake always runs first, so the bar
    // can never come up empty on the first frame.
    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        if (barrel == null || firePoint == null)
        {
            Debug.LogWarning("EnemyTurret: barrel or firePoint not assigned - the turret will not aim.");
        }
    }

    void Update()
    {
        if (player == null || barrel == null || firePoint == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > detectionRange) return;

        // Aim from the MUZZLE, not the pivot. The muzzle sits off the pivot axis, so aiming from
        // the pivot leaves the shot travelling parallel to the player and missing by the muzzle
        // offset - constantly, at any range. See docs/decisions.md #9. This is a fixed-point
        // iteration (rotating the barrel moves the muzzle) and settles within a frame or two.
        Vector3 direction = player.position - firePoint.position;

        if (direction.sqrMagnitude > 0.01f)
        {
            // LookRotation's default up hint is degenerate when the target is straight overhead.
            // Fall back to world forward, which can never be parallel to a near-vertical aim.
            // (Not barrel.forward - that tracks the aim, so it is parallel in exactly the case
            // this guards against.) Only the roll is ill-defined, and both the capsule mesh and
            // the on-axis muzzle are roll-symmetric, so this is correctness, not a visible fix.
            Vector3 upHint = Mathf.Abs(Vector3.Dot(direction.normalized, Vector3.up)) > 0.99f
                ? Vector3.forward
                : Vector3.up;

            Quaternion targetRotation = Quaternion.LookRotation(direction, upHint);
            barrel.rotation = Quaternion.RotateTowards(barrel.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        if (Time.time >= nextFireTime)
        {
            Fire();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Fire()
    {
        if (projectilePrefab == null || firePoint == null) return;

        // firePoint has an identity local rotation, so firePoint.rotation == barrel.rotation and the
        // projectile's forward is the aim direction. That is what the +Z-is-forward convention buys
        // us - do not rotate the barrel by a compensating offset. See docs/decisions.md #9.
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        // Tell the projectile who fired it so it can filter out self-hits. This script sits on the
        // empty `turret` parent, so its hierarchy covers Turret_Base and Turret_Barrel.
        EnemyProjectile proj = projectile.GetComponent<EnemyProjectile>();
        if (proj != null) proj.owner = transform;
    }

    public void TakeDamage(int amount)
    {
        if (currentHealth <= 0) return; // already dying this frame - don't count it twice

        // Only the HP actually removed counts toward the run stats, so overkill does not inflate them.
        GameSession.RecordDamage(Mathf.Min(amount, currentHealth));

        currentHealth -= amount;
        Debug.Log("Turret took damage! Health: " + currentHealth);

        if (currentHealth <= 0)
        {
            Debug.Log("Turret destroyed!");
            GameSession.TurretsDestroyed++;
            Destroy(gameObject);
        }
    }
}
