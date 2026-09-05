using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// A short red boom at a rocket's impact point. Built entirely in code and destroyed by its own
/// timer: RocketProjectile calls Destroy on itself in the same frame, so the effect cannot live on
/// the rocket and cannot be a child of whatever was hit either.
///
/// No prefab, by convention - prefab YAML is not hand-edited in this repo (CLAUDE.md), and the whole
/// point of the DroneHUD / TurretHealthBar pattern is that a feature is one reviewable C# file
/// instead of an opaque asset diff. No pooling either: the only per-hit allocation worth caring
/// about is the material and the texture, and FxAssets already shares those.
///
/// Do NOT move Build into Awake(). AddComponent runs Awake synchronously *inside the call*, so the
/// spawner would not have assigned its fields yet; Start() runs afterwards, which is exactly the gap
/// RocketProjectile.SpawnImpactFx writes into. (RocketProjectile.Awake carries the opposite version
/// of this note, for the opposite reason.)
/// </summary>
public class ImpactExplosion : MonoBehaviour
{
    [Header("Core Flash")]
    [Tooltip("HDR. Reaches the GPU through the material, not the particle colour - see the note on Emit(). Bloom threshold in SampleSceneProfile is 1, so a channel must exceed it to glow.")]
    public Color coreColor = new Color(8f, 1f, 0.25f, 1f);

    [Tooltip("Peak diameter of the flash, in metres.")]
    public float coreSize = 3f;

    [Tooltip("Short and punchy, not a lingering fire. Above ~0.25 it starts to read as a fireball.")]
    public float coreLife = 0.16f;

    [Header("Sparks")]
    public Color sparkColor = new Color(6f, 0.7f, 0.2f, 1f);
    public int sparkCount = 30;
    public float sparkSpeed = 12f;
    public float sparkLife = 0.3f;

    [Tooltip("Cone half-angle in degrees around this object's +Z, which RocketProjectile aims along the surface normal it hit.")]
    [Range(5f, 90f)]
    public float sparkSpread = 55f;

    [Header("Light")]
    [Tooltip("A brief point light so the boom lights the ground around it.")]
    public bool flash = true;
    public float flashRange = 10f;

    [Tooltip("Not a physical unit in this project's URP setup - tune it in Play mode.")]
    public float flashIntensity = 14f;
    public float flashLife = 0.12f;

    [Header("Scale")]
    [Tooltip("Multiplies every size and distance above. One knob for a bigger boom.")]
    public float scale = 1f;

    private Light flashLight;
    private float flashUntil;

    // One diagnostic per session, not per hit. particleCount one frame after emitting is the number
    // that splits any remaining "I see nothing" cleanly into "never emitted" and "emitted but not
    // drawn" - every other failure mode in this shader is silent, so it is worth the one line.
    private static bool logged;
    private ParticleSystem core;
    private int diagnosticFrames;

    void Start()
    {
        core = BuildCore();
        BuildSparks();

        if (flash) BuildFlash();

        // Self-destruct. The rocket is already gone and nothing else owns this object.
        Destroy(gameObject, Mathf.Max(coreLife, sparkLife) + 0.15f);
    }

    void Update()
    {
        if (flashLight != null)
        {
            float remaining = flashUntil - Time.time;
            flashLight.intensity = remaining > 0f ? flashIntensity * (remaining / flashLife) : 0f;
        }

        if (!logged && core != null && ++diagnosticFrames == 2)
        {
            logged = true;
            Debug.Log("ImpactExplosion: core particleCount=" + core.particleCount
                      + " isPlaying=" + core.isPlaying + " at " + transform.position);
        }
    }

    ParticleSystem BuildCore()
    {
        ParticleSystem ps = NewSystem("Core", coreColor);

        // ParticleSystem modules are structs that WRAP a pointer to the native system. Writing
        // `ps.main.startColor = x` is CS1612 - assigning to a returned value. Copying the module into
        // a local and writing through that is not a workaround, it IS the API: the copy still points
        // at the same system. Same story for every other module below.
        //
        // main.duration is deliberately NOT set. It can only be assigned on a stopped system, it is
        // meaningless for a system that is emitted explicitly, and it was the source of "Setting the
        // duration while system is still playing is not supported".
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = coreLife;
        main.startSize = coreSize * scale;
        main.startColor = Color.white;   // 0-1 only; the HDR tint rides the material - see NewSystem
        main.startSpeed = 0f;
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;  // the boom does not follow anything
        main.maxParticles = 8;

        var emission = ps.emission;
        emission.rateOverTime = 0f;   // everything comes from the explicit Emit() below

        var shape = ps.shape;
        shape.enabled = false;   // one particle, dead centre

        // Snaps open, then holds and eases back. A linear ramp reads as a balloon inflating.
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.35f),
            new Keyframe(0.25f, 1f),
            new Keyframe(1f, 0.85f)));

        var color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(FadeOut());

        Fire(ps, 1);
        return ps;
    }

    void BuildSparks()
    {
        ParticleSystem ps = NewSystem("Sparks", sparkColor);

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(sparkLife * 0.45f, sparkLife);
        main.startSpeed = new ParticleSystem.MinMaxCurve(sparkSpeed * 0.4f, sparkSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f * scale, 0.16f * scale);
        main.startColor = Color.white;
        main.gravityModifier = 1.2f;   // sparks arc down; without this it reads as a starburst decal
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 256;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        // A cone emits along its local +Z, and this object's +Z is the surface normal the sweep test
        // returned - see RocketProjectile.SpawnImpactFx.
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = sparkSpread;
        shape.radius = 0.05f * scale;

        var color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(FadeOut());

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        // Stretched billboards read as sparks; plain billboards read as confetti.
        ParticleSystemRenderer r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = 0.06f;
        r.lengthScale = 2.5f;

        Fire(ps, Mathf.Clamp(sparkCount, 0, 256));
    }

    // Play() then Emit(), rather than a burst at t=0. A burst depends on the system's clock still
    // being at 0 when the first simulation step runs, which is exactly the assumption that broke -
    // AddComponent starts the system playing before any of this configuration lands. Emit(n) adds
    // the particles right now, using the current main and shape modules, so nothing can miss them.
    static void Fire(ParticleSystem ps, int count)
    {
        ps.Play();
        if (count > 0) ps.Emit(count);
    }

    void BuildFlash()
    {
        GameObject go = new GameObject("Flash");
        go.transform.SetParent(transform, false);

        flashLight = go.AddComponent<Light>();
        flashLight.type = LightType.Point;
        flashLight.color = new Color(1f, 0.35f, 0.15f, 1f); // LDR - Light.color is not an HDR slot
        flashLight.range = flashRange * scale;
        flashLight.intensity = flashIntensity;
        flashLight.shadows = LightShadows.None;             // a 0.12 s light does not need a shadowmap

        flashUntil = Time.time + flashLife;
    }

    // AddComponent<ParticleSystem> drags in a ParticleSystemRenderer through [RequireComponent], and
    // that renderer arrives with the *builtin* default particle material - MAGENTA under URP.
    // Assigning sharedMaterial is the whole fix, and it must be sharedMaterial rather than material:
    // `material` instantiates a private copy per renderer, which would be a Material leak per hit.
    ParticleSystem NewSystem(string name, Color hdrTint)
    {
        GameObject go;
        if (name == "Core")
        {
            go = gameObject;
        }
        else
        {
            go = new GameObject(name);
            go.transform.SetParent(transform, false); // inherits the impact rotation, so the cone aims right
        }

        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        // AddComponent creates the system with playOnAwake ALREADY true, so it is playing before this
        // line. Stopping and clearing resets the clock and unblocks the module setters.
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();

        r.sharedMaterial = FxAssets.AdditiveDot;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.alignment = ParticleSystemRenderSpace.View;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;

        // The HDR colour goes here, NOT on main.startColor: the particle vertex stream is Color32, so
        // anything above 1 would be clamped and could never cross Bloom's threshold.
        FxAssets.Tint(r, hdrTint);

        return ps;
    }

    // Full alpha for the first 40% of life, then out. Holding before the fade is what makes the boom
    // land as a hit rather than dissolve. Alpha only - it drives the additive SrcAlpha factor, so the
    // colour stays hot and the particle drops out of the bloom threshold cleanly as it dies.
    static Gradient FadeOut()
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.4f),
                new GradientAlphaKey(0f, 1f)
            });
        return g;
    }
}
