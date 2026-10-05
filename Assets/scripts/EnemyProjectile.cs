using UnityEngine;

/// <summary>
/// Turret projectile. On its own this is the GREY turret's shot and today's enemy_rocket: a
/// kinematic trigger flying straight, which tunnels at its scene speed on purpose
/// (docs/decisions.md #14). The other turret types subclass it - ElectricProjectile,
/// ExplosiveProjectile, HomingProjectile - and switch on `useSweep`, because a homing missile or a
/// shell that has to explode on the ground cannot work while it passes through things.
/// See docs/decisions.md #22.
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    public float speed = 20f;
    public float lifeTime = 5f;

    [Tooltip("How long a hit takes control away from the drone. The drone has no health - see docs/decisions.md #10.")]
    public float stunDuration = 1f;

    [Tooltip("Which drone effect the stun plays. The HUD border shows for every kind.")]
    public DroneControls.StunKind stunKind = DroneControls.StunKind.Rock;

    [Tooltip("The turret that fired this projectile. Set by EnemyTurret.Fire() right after Instantiate.")]
    public Transform owner;

    [Header("Hit Detection")]
    [Tooltip("Off: the discrete trigger only, which tunnels at high speed (the grey turret, #14). On: raycast the segment crossed each step, like RocketProjectile - exact at any speed.")]
    public bool useSweep = false;

    [Tooltip("0 = a thin ray along the centre line. Above 0 the sweep is a sphere this wide, for big projectiles (the boss's rocks) whose visible size should count.")]
    public float sweepRadius = 0f;

    [Header("Look")]
    [Tooltip("Optional model shown instead of this prefab's own mesh - e.g. the grey turret's rock. Visual only: the collider stays the prefab's.")]
    public GameObject visualPrefab;

    [Tooltip("World size of the visual relative to the visual prefab's own size. The projectile root's non-uniform scale is cancelled, so 1 = the prefab as authored (the boss's rock uses 3).")]
    public float visualScale = 1f;

    [Tooltip("Top tumble speed of the visual in degrees per second. Each shot turns around X, Y and Z at once, each axis at its own random 50-100% of this and a random direction, from a random starting orientation. 0 = no spin.")]
    public float visualSpin = 360f;

    protected Rigidbody rb;

    // The spinning model and its per-shot rates (deg/s) around its own X, Y and Z. Null without a
    // visualPrefab.
    Transform visual;
    Vector3 spinRates;

    // Set by whichever hit path fires first; stops the sweep and the trigger acting twice.
    protected bool consumed;

    // Shared across every projectile in flight: FixedUpdate runs one sweep at a time and reads the
    // results before the next call. Same arrangement as RocketProjectile.
    static readonly RaycastHit[] SweepBuffer = new RaycastHit[8];

    // Awake, not Start: Awake runs synchronously inside Instantiate, so no physics step can ever
    // observe this projectile as a non-trigger body. That matters because kinematic-vs-*dynamic*
    // does raise OnCollisionEnter (it is kinematic-vs-static that does not), and the drone now
    // reloads the scene on any collision.
    protected virtual void Awake()
    {
        // Hits are detected through trigger events, not OnCollisionEnter: a kinematic Rigidbody
        // generates no collisions against static colliders. See docs/decisions.md #8.
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate; // motion runs in FixedUpdate (50 Hz)

        GetComponent<Collider>().isTrigger = true;
    }

    protected virtual void Start()
    {
        Invoke(nameof(Expire), lifeTime);

        if (owner == null)
        {
            Debug.LogWarning(GetType().Name + ": owner not set - the projectile will destroy itself on its own turret.");
        }

        if (visualPrefab != null)
        {
            MeshRenderer own = GetComponent<MeshRenderer>();
            if (own != null) own.enabled = false;

            // The projectile root is a squashed rod (0.1 x 0.1 x 0.99 on enemy_rock), and a child
            // inherits that - the rock would render as a needle, and spinning it would shear it.
            // So the model sits under a holder that divides the scale back out: the holder's world
            // matrix is a plain rotation, and the model can tumble freely inside it. Same trap as
            // decision #4. The holder must keep an identity local rotation.
            Transform holder = new GameObject("VisualHolder").transform;
            holder.SetParent(transform, false);
            Vector3 parent = transform.lossyScale;
            holder.localScale = new Vector3(1f / parent.x, 1f / parent.y, 1f / parent.z);

            visual = Instantiate(visualPrefab, holder).transform;
            visual.localPosition = Vector3.zero;
            visual.localRotation = Random.rotationUniform;
            visual.localScale = visualPrefab.transform.localScale * visualScale;
            spinRates = new Vector3(RandomRate(), RandomRate(), RandomRate());
        }
    }

    // Spin is cosmetic, so it runs per frame rather than per physics step: smooth at any frame
    // rate, and it stops by itself while paused (timeScale 0).
    // Applying all three per-axis turns every frame compounds them, so the effective axis keeps
    // wandering - a tumble, not a steady spin around one tilted axis.
    void Update()
    {
        if (visual != null) visual.Rotate(spinRates * Time.deltaTime, Space.Self);
    }

    float RandomRate()
    {
        float rate = visualSpin * Random.Range(0.5f, 1f);
        return Random.value < 0.5f ? -rate : rate;
    }

    void FixedUpdate()
    {
        if (consumed) return;

        Steer();

        Vector3 from = rb.position;
        Vector3 dir = transform.forward;
        float step = speed * Time.fixedDeltaTime;

        // Without the sweep, the trigger alone detects hits: reliable only while the step is shorter
        // than the 0.986 m collider (~49 m/s), which the grey turret deliberately exceeds (#14).
        if (useSweep && Sweep(from, dir, step, out RaycastHit hit))
        {
            rb.position = hit.point;
            Hit(hit.collider, hit.point, hit.normal);
            return;
        }

        rb.MovePosition(from + dir * step);
    }

    // Subclasses turn here before the step is taken. Straight flight by default.
    protected virtual void Steer() { }

    // Nearest non-owner hit along the segment, allocation-free - the same rules as
    // RocketProjectile.Sweep: triggers ignored (the only ones are other projectiles), nearest of all
    // because RaycastNonAlloc does not sort.
    bool Sweep(Vector3 from, Vector3 dir, float distance, out RaycastHit nearest)
    {
        nearest = default;

        int count = sweepRadius > 0f
            ? Physics.SphereCastNonAlloc(from, sweepRadius, dir, SweepBuffer, distance, ~0, QueryTriggerInteraction.Ignore)
            : Physics.RaycastNonAlloc(from, dir, SweepBuffer, distance, ~0, QueryTriggerInteraction.Ignore);
        float best = float.PositiveInfinity;
        bool found = false;

        for (int i = 0; i < count; i++)
        {
            if (owner != null && SweepBuffer[i].transform.IsChildOf(owner)) continue;
            if (SweepBuffer[i].distance >= best) continue;

            best = SweepBuffer[i].distance;
            nearest = SweepBuffer[i];
            found = true;

            // A sphere that starts already overlapping reports distance 0 and point (0,0,0) - use
            // the start of the step instead, or the projectile would teleport to the world origin.
            if (nearest.distance <= 0f)
            {
                nearest.point = from;
                nearest.normal = -dir;
            }
        }

        return found;
    }

    // The only hit path for the grey turret; the fallback behind the sweep for the others (catches
    // something moving INTO the projectile between steps).
    void OnTriggerEnter(Collider other)
    {
        // Own turret - IsChildOf is true for owner itself, so this covers the whole hierarchy
        // including the empty `turret` parent, which is Untagged and carries its own collider.
        if (owner != null && other.transform.IsChildOf(owner)) return;

        // Other projectiles and yellow ring gates are triggers - fly through them.
        if (other.isTrigger) return;

        Hit(other, transform.position, -transform.forward);
    }

    void Hit(Collider other, Vector3 point, Vector3 normal)
    {
        if (consumed) return;
        consumed = true;

        OnImpact(point, normal, other);
        Destroy(gameObject);
    }

    // What the projectile does where it lands. Base: stun the drone if it was the drone.
    protected virtual void OnImpact(Vector3 point, Vector3 normal, Collider other)
    {
        // GetComponentInParent, not GetComponent: same convention as docs/decisions.md #4.
        if (other.CompareTag("Player"))
        {
            DroneControls drone = other.GetComponentInParent<DroneControls>();
            if (drone != null) drone.Stun(stunDuration, stunKind);
        }
    }

    // Lifetime ran out without hitting anything. Invoke rather than Destroy(gameObject, lifeTime) so
    // subclasses can leave something behind (the homing missile fizzles).
    void Expire()
    {
        if (consumed) return;
        consumed = true;

        OnExpire();
        Destroy(gameObject);
    }

    protected virtual void OnExpire() { }

    // Shared by the subclasses: an unparented ImpactExplosion, recoloured and resized. Fields are
    // assigned between AddComponent and its Start(), which is why ImpactExplosion builds in Start.
    protected static void SpawnBoom(string name, Vector3 point, Vector3 normal, Color hdrColor, float scale)
    {
        GameObject fx = new GameObject(name);
        fx.transform.SetPositionAndRotation(point + normal * 0.15f, Quaternion.LookRotation(normal));

        ImpactExplosion boom = fx.AddComponent<ImpactExplosion>();
        boom.coreColor = hdrColor;
        boom.sparkColor = new Color(hdrColor.r * 0.75f, hdrColor.g * 0.75f, hdrColor.b * 0.75f, hdrColor.a);
        boom.scale = scale;
    }

    // The drone, if it exists and has not been destroyed. Tag contract, same as EnemyTurret.
    protected static DroneControls FindDrone()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.GetComponent<DroneControls>() : null;
    }
}
