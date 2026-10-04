using UnityEngine;

/// <summary>
/// BLUE turret: where the bolt lands, a zap jumps to the drone if it is within `zapRange` and
/// stuns it with the electric look (the arcs that used to be every stun's effect). A direct hit
/// stuns too. See docs/decisions.md #22.
/// </summary>
public class ElectricProjectile : EnemyProjectile
{
    [Header("Zap")]
    [Tooltip("How far from the impact point the zap reaches for the drone, in metres. Tune by play.")]
    public float zapRange = 8f;

    [Tooltip("HDR colour of the impact flash and the zap.")]
    public Color zapColor = new Color(1.2f, 2.2f, 6f, 1f);

    protected override void OnImpact(Vector3 point, Vector3 normal, Collider other)
    {
        SpawnBoom("ElectricImpact", point, normal, zapColor, 0.6f);

        DroneControls drone = other.GetComponentInParent<DroneControls>();
        if (drone != null)
        {
            // Direct hit - no jump needed.
            drone.Stun(stunDuration, stunKind);
            return;
        }

        drone = FindDrone();
        if (drone == null || drone.HasCrashed) return;

        Vector3 target = drone.transform.position;
        if ((target - point).sqrMagnitude > zapRange * zapRange) return;

        ElectricZap.Spawn(point, target, zapColor);
        drone.Stun(stunDuration, stunKind);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.7f, 1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, zapRange);
    }
}
