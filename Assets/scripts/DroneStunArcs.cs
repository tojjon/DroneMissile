using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Electric arcs crackling around the drone while it is stunned. Built in code and polled once a
/// frame - the same shape as DroneHUD and TurretHealthBar, for the same reasons: no event bus, no
/// committed assets, one reviewable file (docs/decisions.md #12, #13).
///
/// Polling DroneControls.IsStunned is not merely convenient, it is what enforces the rule: Stun()
/// returns early during stunImmunity i-frames without touching stunnedUntil, so a rejected hit
/// leaves IsStunned false and the arcs never fire. Identical to the HUD border rule in #12, and the
/// two therefore ride exactly the same beat - the border tells you the state, these tell you why.
///
/// THREE THINGS HERE LOOK WRONG ON PAPER AND ARE NOT:
///
/// 1. `simulationSpace = World`, which is the entire point of the effect (docs/decisions.md #17).
///    Particles are left behind as the drone flies, so they stream past the lens and PARALLAX. That
///    parallax is the difference between an effect that lives in the world and one that reads as a
///    HUD overlay - which is exactly how the LineRenderer version this replaced looked, being
///    screen-aligned and locked to the camera.
///
/// 2. The rig is an UNPARENTED root object whose pose is copied in LateUpdate, rather than a child
///    of the drone. The drone root's scale is (0.61325, 0.38720787, 1) - a child would inherit that
///    squash, the same trap as docs/decisions.md #4, and particle startSize is driven by lossy
///    scale. TurretHealthBar gets to be a child because the turret root's scale is 1; this does not.
///
/// 3. The emission shell is big and centred slightly ahead of the drone rather than tight on the
///    hull. Main Camera sits at drone-local (0, 0.12, 0.629) with a 0.3 m near clip while the body
///    is a 1 m cube at the origin, so the whole hull is BEHIND the camera plane and anything drawn
///    on it is invisible in first person. The shell is sized so its nearest particles still clear
///    the near clip. OnDrawGizmosSelected draws it.
/// </summary>
[RequireComponent(typeof(DroneControls))]
public class DroneStunArcs : MonoBehaviour
{
    [Header("Kind")]
    [Tooltip("Plays only for stuns of this kind. The drone carries one instance per kind: Electric is the arcs below with their defaults, Rock is a second instance retuned as yellow debris (no noise, no trails, gravity). See docs/decisions.md #22.")]
    public DroneControls.StunKind kind = DroneControls.StunKind.Electric;

    [Header("Arcs")]
    [Tooltip("HDR. Reaches the GPU through the material, not startColor - the particle vertex stream is Color32 and would clamp this to 1, so it could never cross Bloom's threshold of 1.")]
    public Color arcColor = new Color(1.2f, 2.2f, 6f, 1f);

    [Tooltip("Particles per second at full intensity. Each one drags a trail, so this is far denser than it sounds.")]
    public float arcRate = 70f;

    [Tooltip("Extra particles emitted the instant a stun lands, for a sharp crack rather than a fade-in.")]
    public int onsetBurst = 25;

    [Tooltip("How long one arc lives. Short - these are sparks, not embers.")]
    public float arcLifeMin = 0.18f;
    public float arcLifeMax = 0.45f;

    [Tooltip("Particle size range in metres. Small for sparks; the rock instance uses chunkier debris.")]
    public float sizeMin = 0.04f;
    public float sizeMax = 0.10f;

    [Tooltip("0 for electricity, which does not fall. The rock instance drops its debris.")]
    public float gravity = 0f;

    [Header("Field (drone-local metres)")]
    [Tooltip("Centre of the emission shell, in drone-local space. Slightly ahead of the origin so more of it lands in view - see the class comment on why it is not on the hull.")]
    public Vector3 fieldCenter = new Vector3(0f, 0f, 0.3f);

    [Tooltip("Outer radius of the shell. Nearest particles must stay clear of the camera's 0.3 m near clip.")]
    public float fieldRadius = 1.8f;

    [Tooltip("Shell thickness as a fraction of the radius. 1 fills the sphere solid; low values keep particles off the lens and out of the drone.")]
    [Range(0.05f, 1f)]
    public float fieldThickness = 0.45f;

    [Header("Crackle")]
    [Tooltip("How violently the noise field throws particles around. This is what reads as electric rather than as a spark shower. 0 turns the noise module off.")]
    public float noiseStrength = 1.8f;

    [Tooltip("Higher frequency means tighter, more jagged deflection.")]
    public float noiseFrequency = 1.6f;
    public float noiseScrollSpeed = 2.5f;

    [Tooltip("Trails are what you actually read as arcs. Off for debris.")]
    public bool useTrails = true;

    [Tooltip("Length of the trail behind each particle, in seconds.")]
    public float trailLifetime = 0.25f;

    [Range(0.05f, 1f)]
    public float trailWidth = 0.35f;

    [Header("Fade")]
    [Tooltip("Intensity per second while ramping. In fast, out slow - the same shape as the HUD stun border, so the two die together.")]
    public float fadeInSpeed = 12f;
    public float fadeOutSpeed = 3f;

    private DroneControls drone;
    private Transform rig;              // unparented, scale 1, follows the drone
    private ParticleSystem arcs;

    private float intensity;            // 0..1, drives emission rate

    void Start()
    {
        drone = GetComponent<DroneControls>();
        Build();
    }

    // LateUpdate, not Update, for the same reason TurretHealthBar uses it: the drone moves in
    // FixedUpdate under Interpolate, so its interpolated visual pose is only final this late.
    // Reading it in Update leaves the emitter a frame behind the drone while it rolls.
    void LateUpdate()
    {
        float target = drone.IsStunned && drone.LastStunKind == kind ? 1f : 0f;
        float speed = target > intensity ? fadeInSpeed : fadeOutSpeed;
        intensity = Mathf.MoveTowards(intensity, target, speed * Time.deltaTime);

        bool live = intensity > 0.001f;
        if (rig.gameObject.activeSelf != live)
        {
            rig.gameObject.SetActive(live);

            if (live)
            {
                // Play() does nothing on a disabled GameObject, and playOnAwake fires only on the
                // FIRST Awake - relying on it would leave every stun after the first one dead.
                arcs.Play();

                // Emit() rather than a burst: the crack has to land on the frame the stun does, and
                // a burst scheduled at t=0 depends on the system clock still being there.
                if (onsetBurst > 0) arcs.Emit(onsetBurst);
            }
        }
        if (!live) return;

        // Only the emitter follows the drone. Particles already in flight are in world space and
        // stay where they were born, which is what makes them stream past the camera.
        rig.SetPositionAndRotation(transform.position, transform.rotation);

        // Module structs wrap a pointer to the native system, so writing through a local copy
        // reaches the real thing. `arcs.emission.rateOverTime = x` would not compile (CS1612).
        var emission = arcs.emission;
        emission.rateOverTime = arcRate * intensity;

        // No explicit fade-out beyond this: emission stops and the particles in flight finish their
        // own lives, which dies out more naturally than ramping alpha on everything at once.
    }

    void Build()
    {
        // No parent: see the class comment. Created at runtime, so it belongs to the active scene and
        // dies with it on SceneManager.LoadScene, like everything else here.
        GameObject rigGo = new GameObject("DroneStunArcs_" + kind);
        rig = rigGo.transform;
        rig.SetPositionAndRotation(transform.position, transform.rotation);

        arcs = BuildArcs();

        // Deactivated last, after the system is fully configured. Nothing to show until a stun.
        rigGo.SetActive(false);
    }

    ParticleSystem BuildArcs()
    {
        GameObject go = new GameObject("Arcs");
        go.transform.SetParent(rig, false);
        go.transform.localPosition = fieldCenter;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        // AddComponent creates the system already playing (playOnAwake defaults on), which blocks
        // some module setters and leaves the clock in an unknown place. Stop and clear first.
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;                 // LateUpdate calls Play() when the rig comes back
        main.startLifetime = new ParticleSystem.MinMaxCurve(arcLifeMin, arcLifeMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startColor = Color.white;            // 0-1 only; the HDR tint rides the material
        main.gravityModifier = gravity;           // 0 for electricity, which does not fall
        main.simulationSpace = ParticleSystemSimulationSpace.World;   // the whole point - see class comment
        main.maxParticles = 300;

        var emission = ps.emission;
        emission.rateOverTime = 0f;               // driven from LateUpdate

        // A hollow shell, not a solid sphere: solid would put particles inside the drone and right
        // against the lens, where the near clip eats them anyway.
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = fieldRadius;
        shape.radiusThickness = fieldThickness;

        // THIS is what makes it read as electricity. Straight-line sparks look like a grinder; a
        // strong high-frequency noise field throws each particle along an erratic zig-zag, and the
        // trail behind it becomes the visible arc. It replaces the hand-built jagged polylines the
        // LineRenderer version used, and it moves, which they never did.
        var noise = ps.noise;
        noise.enabled = noiseStrength > 0f;
        noise.strength = noiseStrength;
        noise.frequency = noiseFrequency;
        noise.scrollSpeed = noiseScrollSpeed;
        noise.quality = ParticleSystemNoiseQuality.High;
        noise.damping = false;                    // damping would scale strength with size and calm it down

        var trails = ps.trails;
        trails.enabled = useTrails;
        trails.ratio = 1f;                        // every particle gets one
        trails.lifetime = new ParticleSystem.MinMaxCurve(trailLifetime);
        trails.minVertexDistance = 0.02f;
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(trailWidth);
        trails.dieWithParticles = true;
        trails.sizeAffectsWidth = true;
        trails.inheritParticleColor = true;
        trails.colorOverTrail = new ParticleSystem.MinMaxGradient(FadeOut());

        var color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(FadeOut());

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = FxAssets.AdditiveDot;      // NOT the builtin default - that is magenta under URP
        r.trailMaterial = FxAssets.AdditiveBand;      // the band texture runs U along the trail, V across it
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.alignment = ParticleSystemRenderSpace.View;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;

        // HDR goes on the material, never on startColor - the vertex stream is Color32 and clamps.
        // No index on the property block, so this covers the trail material too.
        FxAssets.Tint(r, arcColor);

        return ps;
    }

    // Alpha only, so the additive SrcAlpha factor scales the contribution while the colour stays
    // hot. Particles therefore drop out of the bloom threshold as they die rather than turning grey.
    static Gradient FadeOut()
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        return g;
    }

    // The rig is unparented, so it does not die with the drone if the drone is ever destroyed. Today
    // nothing destroys the drone (docs/decisions.md #10 - it crashes and reloads the scene instead),
    // but leaving that dependent on a comment is how orphans happen.
    void OnDestroy()
    {
        if (rig != null) Destroy(rig.gameObject);
    }

    // Editor-only feedback: the shell is the hardest thing here to tune blind. Built with an explicit
    // unit-scale matrix rather than transform.localToWorldMatrix, because that one carries the
    // drone's non-uniform scale and would draw a squashed shell - the very thing the rig avoids.
    void OnDrawGizmosSelected()
    {
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.color = new Color(0.4f, 0.7f, 1f, 0.8f);
        Gizmos.DrawWireSphere(fieldCenter, fieldRadius);
        Gizmos.color = new Color(0.4f, 0.7f, 1f, 0.35f);
        Gizmos.DrawWireSphere(fieldCenter, fieldRadius * (1f - fieldThickness));
    }
}
