using UnityEngine;

public class RocketProjectile : MonoBehaviour
{
    public float speed = 30f;
    public float lifeTime = 5f;

    [Tooltip("Damage to a turret. Shoting raises it above 10 for the shot after flying through yellow rings.")]
    public int damage = 10;

    [Header("Boosted Blink")]
    [Tooltip("A rocket carrying a ring bonus blinks this colour. HDR so it blooms.")]
    public Color boostColor = new Color(4f, 3.2f, 0.3f, 1f);
    public float blinkRate = 12f;

    [Tooltip("The object that fired this rocket. Set by Shoting.Fire() right after Instantiate.")]
    public Transform owner;

    // Upgrades (docs/plans/upgrades.md) - all set by Shoting at fire time, never in the prefab.
    // Homing: steer toward this turret at homingTurnRate deg/s. Null = unguided, the default and
    // the reason the crosshair is exact (decisions #12); Homing is the deliberate exception.
    [System.NonSerialized] public EnemyTurret homingTarget;
    [System.NonSerialized] public float homingTurnRate;

    // Lightning: on a turret hit, zap this many further turrets within chainRange.
    [System.NonSerialized] public int chainJumps;
    [System.NonSerialized] public float chainRange;
    [System.NonSerialized] public float chainDamageFraction;

    // Ring bonus on this rocket - what makes it blink. A flag rather than damage > 10, because the
    // +1 Damage upgrade raises damage too and must not make every rocket blink.
    [System.NonSerialized] public bool boosted;

    // Bouncy: reflect off anything that is not a turret, this many times.
    [System.NonSerialized] public int bouncesLeft;

    // Fire and Freeze: applied to every turret this rocket damages, directly or through a Lightning
    // jump (Viktor, 06.10.2026). Zero = not owned.
    [System.NonSerialized] public float burnSeconds;
    [System.NonSerialized] public int burnTickDamage;
    [System.NonSerialized] public float burnTickInterval;
    [System.NonSerialized] public float freezeSeconds;

    [Tooltip("Size of the spark left where a Bouncy rocket bounces, relative to impactScale.")]
    public float bounceFxScale = 0.4f;

    [Tooltip("HDR colour of the Lightning upgrade's zaps.")]
    public Color chainColor = new Color(1.2f, 2.2f, 6f, 1f);

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
    private Renderer body;
    private MaterialPropertyBlock blink;

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

        if (boosted)
        {
            body = GetComponent<Renderer>();
            blink = new MaterialPropertyBlock();
        }
    }

    // Boosted rockets only. A property block, so no material is instantiated per rocket.
    void Update()
    {
        if (blink == null || body == null) return;

        bool on = Mathf.Repeat(Time.time * blinkRate, 1f) < 0.5f;
        blink.Clear();
        if (on)
        {
            blink.SetColor("_BaseColor", boostColor);
            blink.SetColor("_EmissionColor", boostColor);
        }
        body.SetPropertyBlock(blink);
    }

    void FixedUpdate()
    {
        if (consumed) return;

        Steer();

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
            // Bouncy: walls, floor, ceiling and terrain reflect the rocket; a turret is always a hit.
            if (bouncesLeft > 0 && hit.collider.GetComponentInParent<EnemyTurret>() == null)
            {
                Bounce(hit, dir);
                return;
            }

            rb.position = hit.point;
            HandleHit(hit.collider, hit.point, hit.normal);
            return;
        }

        rb.MovePosition(from + dir * step);
    }

    // Homing upgrade: turn toward the locked turret, limited to homingTurnRate - the same steering
    // as the enemy HomingProjectile. A target that died just leaves the rocket flying straight.
    void Steer()
    {
        if (homingTarget == null || !homingTarget.Alive) return;

        Vector3 toTarget = homingTarget.AimPoint - rb.position;
        if (toTarget.sqrMagnitude < 0.0001f) return;

        rb.rotation = Quaternion.RotateTowards(rb.rotation, Quaternion.LookRotation(toTarget),
                                               homingTurnRate * Time.fixedDeltaTime);
        transform.rotation = rb.rotation;
    }

    // Lifted a hair off the surface so the next sweep cannot start inside it. The rest of this step's
    // distance is dropped - 2.4 m at most, invisible at 120 m/s.
    void Bounce(RaycastHit hit, Vector3 dir)
    {
        bouncesLeft--;
        SpawnImpactFx(hit.point, hit.normal, bounceFxScale);

        Vector3 pos = hit.point + hit.normal * 0.05f;
        Quaternion rot = Quaternion.LookRotation(Vector3.Reflect(dir, hit.normal));
        rb.position = pos;
        rb.rotation = rot;
        transform.SetPositionAndRotation(pos, rot);
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

        // Triggers are other projectiles and yellow ring gates - never something to detonate on.
        // Same rule as the sweep's QueryTriggerInteraction.Ignore.
        if (other.isTrigger) return;

        HandleHit(other, transform.position, -transform.forward);
    }

    void HandleHit(Collider other, Vector3 point, Vector3 normal)
    {
        if (consumed) return;
        consumed = true;

        Debug.Log("Rocket hit: " + other.gameObject.name);

        // Before the damage call, so a turret that TakeDamage() destroys cannot cost us the boom.
        SpawnImpactFx(point, normal, 1f);

        // GetComponentInParent, not GetComponent: EnemyTurret sits on the empty parent while the
        // colliders are on its children. See docs/decisions.md #4.
        EnemyTurret turret = other.GetComponentInParent<EnemyTurret>();
        if (turret != null)
        {
            // Captured before the damage: a killing hit queues Destroy, and the chain starts here.
            Vector3 from = turret.AimPoint;
            turret.TakeDamage(damage);
            ApplyAilments(turret);
            if (chainJumps > 0) Chain(turret, from);
        }

        Destroy(gameObject);
    }

    // Lightning upgrade: each jump goes to the nearest turret not yet in the chain, within
    // chainRange of the last one, for a share of the rocket's damage (rounded up).
    void Chain(EnemyTurret first, Vector3 from)
    {
        System.Collections.Generic.List<EnemyTurret> hit = new System.Collections.Generic.List<EnemyTurret> { first };
        int jumpDamage = Mathf.CeilToInt(damage * chainDamageFraction);

        for (int jump = 0; jump < chainJumps; jump++)
        {
            EnemyTurret next = null;
            float best = chainRange * chainRange;
            foreach (EnemyTurret t in FindObjectsByType<EnemyTurret>())
            {
                if (!t.Alive || hit.Contains(t)) continue;
                float d = (t.AimPoint - from).sqrMagnitude;
                if (d <= best) { best = d; next = t; }
            }
            if (next == null) return;

            Vector3 to = next.AimPoint;
            ElectricZap.Spawn(from, to, chainColor);
            next.TakeDamage(jumpDamage);
            ApplyAilments(next);
            hit.Add(next);
            from = to;
        }
    }

    // Fire and Freeze. Both ignore a turret the hit just killed (EnemyTurret checks Alive).
    void ApplyAilments(EnemyTurret t)
    {
        if (burnSeconds > 0f) t.Ignite(burnSeconds, burnTickDamage, burnTickInterval);
        if (freezeSeconds > 0f) t.Freeze(freezeSeconds);
    }

    // `point` and `normal` come from the sweep, so this is the real contact point on the real
    // surface - no ClosestPoint() guesswork (that is undefined for TerrainCollider and non-convex
    // meshes, where it hands back the collider's transform origin) and no flight-path approximation
    // of the normal.
    void SpawnImpactFx(Vector3 point, Vector3 normal, float sizeFactor)
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
        boom.scale = impactScale * sizeFactor;
    }
}
