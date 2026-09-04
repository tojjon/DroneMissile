using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public float speed = 20f;
    public float lifeTime = 5f;

    [Tooltip("How long a hit takes control away from the drone. The drone has no health - see docs/decisions.md #10.")]
    public float stunDuration = 1f;

    [Tooltip("The turret that fired this projectile. Set by EnemyTurret.Fire() right after Instantiate.")]
    public Transform owner;

    private Rigidbody rb;

    // Awake, not Start: Awake runs synchronously inside Instantiate, so no physics step can ever
    // observe this projectile as a non-trigger body. That matters because kinematic-vs-*dynamic*
    // does raise OnCollisionEnter (it is kinematic-vs-static that does not), and the drone now
    // reloads the scene on any collision.
    void Awake()
    {
        // Hits are detected through trigger events, not OnCollisionEnter: a kinematic Rigidbody
        // generates no collisions against static colliders. See docs/decisions.md #8.
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
            // The drone has no health - a hit takes control away instead. See docs/decisions.md #10.
            // GetComponentInParent, not GetComponent: same convention as docs/decisions.md #4.
            DroneControls drone = other.GetComponentInParent<DroneControls>();
            if (drone != null)
            {
                drone.Stun(stunDuration);
            }
        }

        Destroy(gameObject);
    }
}
