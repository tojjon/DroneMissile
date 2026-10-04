using UnityEngine;

/// <summary>
/// GREEN turret: a missile that turns toward the drone. It stays dodgeable (concept pillar "vše je
/// uhýbatelné") because it can only turn `turnRate` degrees a second and burns out after
/// `lifeTime` - a hard break or simply outrunning it beats it. See docs/decisions.md #22.
/// </summary>
public class HomingProjectile : EnemyProjectile
{
    [Header("Homing")]
    [Tooltip("Degrees per second the missile can turn. Lower is easier to out-turn.")]
    public float turnRate = 70f;

    [Tooltip("HDR colour of the fizzle when it burns out.")]
    public Color fizzleColor = new Color(0.6f, 6f, 1f, 1f);

    private Transform target;

    protected override void Start()
    {
        base.Start();

        DroneControls drone = FindDrone();
        if (drone != null) target = drone.transform;
    }

    protected override void Steer()
    {
        if (target == null) return;

        Vector3 toTarget = target.position - rb.position;
        if (toTarget.sqrMagnitude < 0.0001f) return;

        // The rotation drives the flight: the base class moves along transform.forward. rb.rotation
        // rather than MoveRotation, so the turn lands before this same step's movement.
        Quaternion want = Quaternion.LookRotation(toTarget);
        rb.rotation = Quaternion.RotateTowards(rb.rotation, want, turnRate * Time.fixedDeltaTime);
        transform.rotation = rb.rotation;
    }

    protected override void OnExpire()
    {
        SpawnBoom("Fizzle", transform.position, -transform.forward, fizzleColor, 0.4f);
    }
}
