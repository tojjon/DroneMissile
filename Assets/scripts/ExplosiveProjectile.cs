using UnityEngine;

/// <summary>
/// RED turret: explodes on touching anything and stuns the drone if it is inside the blast - no
/// direct hit needed, which is what makes it the strongest of the stuns. See docs/decisions.md #22.
/// </summary>
public class ExplosiveProjectile : EnemyProjectile
{
    [Header("Explosion")]
    [Tooltip("Stun radius around the impact point, in metres. Tune by play.")]
    public float blastRadius = 10f;

    [Tooltip("HDR colour of the boom.")]
    public Color blastColor = new Color(8f, 1.2f, 0.3f, 1f);

    // Large enough for the drone's few colliders plus whatever terrain and turret parts are near.
    static readonly Collider[] Overlap = new Collider[32];

    protected override void OnImpact(Vector3 point, Vector3 normal, Collider other)
    {
        // The flash grows with the radius so the danger zone reads at a glance. ImpactExplosion's
        // default core is 3 m across, so radius/1.5 makes the flash about as wide as the blast.
        SpawnBoom("Blast", point, normal, blastColor, Mathf.Max(1f, blastRadius / 1.5f));

        int count = Physics.OverlapSphereNonAlloc(point, blastRadius, Overlap, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            DroneControls drone = Overlap[i].GetComponentInParent<DroneControls>();
            if (drone != null)
            {
                // Once is enough: the drone's i-frames would ignore a second call anyway.
                drone.Stun(stunDuration, stunKind);
                return;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, blastRadius);
    }
}
