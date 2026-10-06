using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// What the Fire and Freeze upgrades look like on a turret (docs/plans/upgrades-2.md): flames while
/// it burns, an icy tint while it is frozen. EnemyTurret adds this on the first ailment; it polls
/// IsBurning / IsFrozen like the health bar polls HealthFraction - no event bus.
///
/// The flames are an UNPARENTED rig whose position is copied in LateUpdate, not a child: the boss's
/// root is scaled x3 and particle startSize follows lossy scale - the trap from docs/decisions.md #4,
/// solved the same way as DroneStunArcs. Built in code like every other effect (decision #16).
/// </summary>
[RequireComponent(typeof(EnemyTurret))]
public class TurretStatusFx : MonoBehaviour
{
    [Tooltip("HDR. Rides the material through FxAssets.Tint - the particle colour stream would clamp it.")]
    public Color fireColor = new Color(6f, 1.6f, 0.25f, 1f);
    public float fireRate = 40f;

    [Tooltip("Multiplies the base colour of every turret renderer while frozen.")]
    public Color frostColor = new Color(0.45f, 0.8f, 1.6f, 1f);

    private EnemyTurret turret;
    private Renderer[] renderers;
    private MaterialPropertyBlock frost;
    private bool tinted;

    private ParticleSystem flames;
    private Vector3 flameOffset;

    void Awake()
    {
        turret = GetComponent<EnemyTurret>();
        renderers = GetComponentsInChildren<Renderer>();
        frost = new MaterialPropertyBlock();
    }

    void Start()
    {
        flames = BuildFlames();
    }

    void LateUpdate()
    {
        bool frozen = turret.IsFrozen;
        if (frozen != tinted) SetFrost(frozen);

        if (flames == null) return;
        flames.transform.position = transform.position + flameOffset;

        bool burning = turret.IsBurning;
        if (burning && !flames.isEmitting) flames.Play();
        else if (!burning && flames.isEmitting) flames.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    // The turret died (or the wave was cleared): let the flames in flight finish their lives.
    void OnDestroy()
    {
        if (flames == null) return;
        flames.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        Destroy(flames.gameObject, 1.5f);
    }

    // Both _BaseColor (URP Lit) and _Color, as in FxAssets.Tint. Clearing the block restores each
    // material's own colour, so nothing about the turret type has to be remembered.
    void SetFrost(bool on)
    {
        tinted = on;
        foreach (Renderer r in renderers)
        {
            if (r == null || r is ParticleSystemRenderer) continue;
            frost.Clear();
            if (on)
            {
                frost.SetColor("_BaseColor", frostColor);
                frost.SetColor("_Color", frostColor);
            }
            r.SetPropertyBlock(on ? frost : null);
        }
    }

    ParticleSystem BuildFlames()
    {
        // Size and place the fire from what the turret actually renders - prefab roots are not at
        // their base (RunManager.RestOnGround), and the boss is three times the size.
        Bounds b = new Bounds(transform.position, Vector3.one);
        bool first = true;
        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            if (first) { b = r.bounds; first = false; }
            else b.Encapsulate(r.bounds);
        }
        flameOffset = b.center - transform.position;
        float radius = Mathf.Max(0.3f, Mathf.Max(b.extents.x, b.extents.z) * 0.7f);
        float scale = Mathf.Clamp(radius, 0.5f, 3f);

        GameObject go = new GameObject("TurretFire");
        go.transform.SetPositionAndRotation(b.center, Quaternion.Euler(-90f, 0f, 0f));   // +Z up

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        // AddComponent hands back a playing system - stop and clear before any module (CLAUDE.md).
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f * scale, 2f * scale);
        main.startSize = new ParticleSystem.MinMaxCurve(0.35f * scale, 0.8f * scale);
        main.startColor = Color.white;   // 0-1 only; the HDR tint rides the material
        main.gravityModifier = -0.15f;   // flames rise
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;

        var emission = ps.emission;
        emission.rateOverTime = fireRate;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f;
        shape.radius = radius;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.1f));

        var color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.45f, 0.2f), 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
        color.color = new ParticleSystem.MinMaxGradient(g);

        ParticleSystemRenderer pr = go.GetComponent<ParticleSystemRenderer>();
        pr.sharedMaterial = FxAssets.AdditiveDot;   // sharedMaterial - the default one is magenta under URP
        pr.renderMode = ParticleSystemRenderMode.Billboard;
        pr.shadowCastingMode = ShadowCastingMode.Off;
        pr.receiveShadows = false;
        FxAssets.Tint(pr, fireColor);

        return ps;
    }
}
