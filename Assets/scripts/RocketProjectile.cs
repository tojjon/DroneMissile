using UnityEngine;

public class RocketProjectile : MonoBehaviour
{
    public float speed = 30f;
    public float lifeTime = 5f;

    [Tooltip("The object that fired this rocket. Set by Shoting.Fire() right after Instantiate.")]
    public Transform owner;

    [Header("Impact FX")]
    [Tooltip("Spawn a red boom where this rocket dies. Every impact - terrain, the slab, the turret.")]
    public bool impactFx = true;

    [Tooltip("HDR. Reaches the GPU through the material, not the particle colour, because the particle vertex stream is Color32 and would clamp this to 1. Bloom threshold is 1, so a channel must exceed it to glow.")]
    public Color impactColor = new Color(8f, 1f, 0.25f, 1f);

    [Tooltip("Overall size of the boom. Multiplies every distance in ImpactExplosion.")]
    public float impactScale = 1f;

    [Tooltip("Lift the effect off the surface along the hit normal, so the flash is not buried inside the ground. The sweep gives an exact contact point, so this only needs to clear the flash radius.")]
    public float impactBackOffset = 0.15f;

    private Rigidbody rb;

    // Shared across every rocket in flight: FixedUpdate runs one sweep at a time and reads the
    // results before the next call, so there is nothing to keep per-instance. Static keeps the
    // per-shot allocation at zero.
    static readonly RaycastHit[] SweepBuffer = new RaycastHit[8];

    // Set by whichever hit path fires first. Both the sweep and OnTriggerEnter route through
    // HandleHit, and this is what stops them acting on the same rocket twice.
    private bool consumed;

    // Awake, not Start: Awake runs synchronously inside Instantiate, so no physics step can ever
    // observe this rocket as a non-trigger body. The rocket spawns partly inside the drone's own
    // collider, kinematic-vs-*dynamic* does raise OnCollisionEnter, and the drone reloads the
    // scene on any collision - a one-step window here would reset the run on every shot.
    void Awake()
    {
        // The trigger collider is now only the fallback path - FixedUpdate sweeps the flight segment
        // with a raycast instead, because at this prefab's speed the discrete trigger misses most
        // hits. It still has to be a trigger: a solid collider here would raise OnCollisionEnter
        // against the drone and reload the run on every shot.
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate; // motion runs in FixedUpdate (50 Hz)

        GetComponent<Collider>().isTrigger = true;
    }
    void Start()
    {
        Destroy(gameObject, lifeTime);

        if (owner == null)
        {
            Debug.LogWarning("RocketProjectile: owner not set - the rocket will destroy itself on its own shooter.");
        }
    }

    void FixedUpdate()
    {
        if (consumed) return;

        Vector3 from = rb.position;
        Vector3 dir = transform.forward;
        float step = speed * Time.fixedDeltaTime;

        // A SWEEP, not the discrete trigger. This prefab runs speed 120 - 2.4 m per physics step
        // against a 0.986 m collider - so successive trigger tests do not overlap and roughly 59% of
        // shots used to pass straight through terrain and turrets with no hit at all. Raycasting the
        // segment the rocket is about to cross makes detection exact at any speed, which supersedes
        // the ~49 m/s ceiling from docs/decisions.md #8 for the PLAYER rocket. The enemy rocket keeps
        // its deliberate tunnelling (#14) and is untouched.
        //
        // The sweep also hands back a real surface normal, which is what orients the spark cone.
        if (Sweep(from, dir, step, out RaycastHit hit))
        {
            rb.position = hit.point;
            HandleHit(hit.collider, hit.point, hit.normal);
            return;
        }

        rb.MovePosition(from + dir * step);
    }

    // Nearest non-owner hit along the segment, allocation-free.
    //
    // QueryTriggerInteraction.Ignore: every collider in SampleScene is solid (verified - all seven
    // are m_IsTrigger: 0), so the only triggers in flight are OTHER projectiles, which must never
    // stop this one. Nearest-of-all rather than the first hit, because RaycastNonAlloc does not sort
    // and the first entry could be the drone the rocket just left.
    bool Sweep(Vector3 from, Vector3 dir, float distance, out RaycastHit nearest)
    {
        nearest = default;

        int count = Physics.RaycastNonAlloc(from, dir, SweepBuffer, distance, ~0, QueryTriggerInteraction.Ignore);
        float best = float.PositiveInfinity;
        bool found = false;

        for (int i = 0; i < count; i++)
        {
            // Own shooter - IsChildOf is true for owner itself, so this covers its whole hierarchy.
            if (owner != null && SweepBuffer[i].transform.IsChildOf(owner)) continue;
            if (SweepBuffer[i].distance >= best) continue;

            best = SweepBuffer[i].distance;
            nearest = SweepBuffer[i];
            found = true;
        }

        return found;
    }

    // Belt and braces. The sweep above is the real hit path now; this still catches the odd case of
    // something moving INTO the rocket between two steps, which a forward-only raycast cannot see.
    // The `consumed` guard is what stops the two paths firing on the same rocket.
    void OnTriggerEnter(Collider other)
    {
        if (consumed) return;
        if (owner != null && other.transform.IsChildOf(owner)) return;

        HandleHit(other, transform.position, -transform.forward);
    }

    void HandleHit(Collider other, Vector3 point, Vector3 normal)
    {
        if (consumed) return;
        consumed = true;

        Debug.Log("Rocket hit: " + other.gameObject.name);

        // Before the damage call, so a turret that TakeDamage() destroys cannot cost us the boom.
        SpawnImpactFx(point, normal);

        // GetComponentInParent, not GetComponent: EnemyTurret sits on the empty parent while the
        // colliders are on its children. See docs/decisions.md #4.
        EnemyTurret turret = other.GetComponentInParent<EnemyTurret>();
        if (turret != null)
        {
            turret.TakeDamage(10);
        }

        Destroy(gameObject);
    }

    // `point` and `normal` come from the sweep, so this is the real contact point on the real
    // surface - no ClosestPoint() guesswork (that is undefined for TerrainCollider and non-convex
    // meshes, where it hands back the collider's transform origin) and no flight-path approximation
    // of the normal.
    void SpawnImpactFx(Vector3 point, Vector3 normal)
    {
        if (!impactFx) return;

        // Unparented: the effect has to outlive this rocket, and parenting it to whatever was hit
        // would inherit that object's scale - the ground slab and the turret parts are non-uniform.
        GameObject fx = new GameObject("RocketImpact");
        fx.transform.SetPositionAndRotation(
            point + normal * impactBackOffset,      // lift it off the surface so it is not buried
            Quaternion.LookRotation(normal));       // +Z is the normal; the spark cone follows it

        // AddComponent runs Awake synchronously but not Start, so these assignments land before
        // ImpactExplosion.Start() reads them. That is why it builds in Start() and not Awake() -
        // the opposite of this class's own Awake(), for the opposite reason.
        ImpactExplosion boom = fx.AddComponent<ImpactExplosion>();
        boom.coreColor = impactColor;
        boom.sparkColor = new Color(impactColor.r * 0.75f, impactColor.g * 0.75f, impactColor.b * 0.75f, impactColor.a);
        boom.scale = impactScale;
    }
}
