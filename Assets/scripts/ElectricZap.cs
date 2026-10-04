using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The blue turret's zap: a short burst of lightning from an impact point to the drone. Built in
/// code like DroneStunArcs and with the same idiom - particles strung along the segment, a strong
/// noise field throwing them into a zig-zag, trails drawing the filament. Not a LineRenderer:
/// docs/decisions.md #17 rules those out for arcs. Destroys itself.
/// </summary>
public class ElectricZap : MonoBehaviour
{
    public Vector3 from;
    public Vector3 to;
    public Color color = new Color(1.2f, 2.2f, 6f, 1f);

    [Tooltip("Particles per metre of zap.")]
    public float density = 6f;
    public float life = 0.25f;

    // Same contract as ImpactExplosion: fields are set between AddComponent and Start().
    public static void Spawn(Vector3 from, Vector3 to, Color hdrColor)
    {
        GameObject go = new GameObject("ElectricZap");
        go.transform.position = from;

        ElectricZap zap = go.AddComponent<ElectricZap>();
        zap.from = from;
        zap.to = to;
        zap.color = hdrColor;
    }

    void Start()
    {
        ParticleSystem ps = gameObject.AddComponent<ParticleSystem>();

        // AddComponent starts the system playing; stop and clear before touching modules (see the
        // CLAUDE.md note - otherwise main.duration is rejected and Emit lands on a running clock).
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = life;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.startColor = Color.white;            // 0-1 only; HDR rides the material
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 600;

        var emission = ps.emission;
        emission.enabled = false;                 // everything comes from Emit below

        var shape = ps.shape;
        shape.enabled = false;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 3f;
        noise.frequency = 2f;
        noise.scrollSpeed = 3f;
        noise.quality = ParticleSystemNoiseQuality.High;
        noise.damping = false;

        var trails = ps.trails;
        trails.enabled = true;
        trails.ratio = 1f;
        trails.lifetime = new ParticleSystem.MinMaxCurve(0.15f);
        trails.minVertexDistance = 0.02f;
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(0.4f);
        trails.dieWithParticles = true;
        trails.sizeAffectsWidth = true;
        trails.inheritParticleColor = true;

        var fade = ps.colorOverLifetime;
        fade.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        fade.color = new ParticleSystem.MinMaxGradient(g);

        ParticleSystemRenderer r = GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = FxAssets.AdditiveDot;      // NOT the builtin default - magenta under URP
        r.trailMaterial = FxAssets.AdditiveBand;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        FxAssets.Tint(r, color);

        // Explicit Play + Emit, never a burst at t = 0 (CLAUDE.md).
        ps.Play();
        Vector3 span = to - from;
        int count = Mathf.Clamp(Mathf.CeilToInt(span.magnitude * density), 4, 500);
        var emit = new ParticleSystem.EmitParams();
        for (int i = 0; i < count; i++)
        {
            emit.position = from + span * ((i + Random.value) / count);
            emit.velocity = Random.insideUnitSphere * 1.5f;
            ps.Emit(emit, 1);
        }

        Destroy(gameObject, life + 0.2f);
    }
}
