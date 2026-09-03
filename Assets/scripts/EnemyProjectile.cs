using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public float speed = 20f;
    public float lifeTime = 5f;
    public int damage = 10;

    [Tooltip("The turret that fired this projectile. Set by EnemyTurret.Fire() right after Instantiate.")]
    public Transform owner;

    private Rigidbody rb;

    void Start()
    {
        Destroy(gameObject, lifeTime);

        // Hits are detected through trigger events, not OnCollisionEnter: a kinematic Rigidbody
        // generates no collisions against static colliders. See docs/decisions.md #8.
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate; // motion runs in FixedUpdate (50 Hz)

        GetComponent<Collider>().isTrigger = true;

        if (owner == null)
        {
            Debug.LogWarning("EnemyProjectile: owner not set - the projectile will destroy itself on its own turret.");
        }
    }

    void FixedUpdate()
    {
        // FixedUpdate, not Update: the 0.4 m step is shorter than the collider (0.986 m along Z),
        // so successive test positions overlap and nothing can slip through. In Update() this
        // would depend on frame rate. Holds up to speed ~49 m/s at a 0.02 fixed timestep.
        rb.MovePosition(rb.position + transform.forward * speed * Time.fixedDeltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        // Own turret - IsChildOf is true for owner itself, so this covers the whole hierarchy
        // including the empty `turret` parent, which is Untagged and carries its own collider.
        if (owner != null && other.transform.IsChildOf(owner)) return;

        if (other.CompareTag("Player"))
        {
            DroneHealth health = other.GetComponent<DroneHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
        }

        Destroy(gameObject);
    }
}
