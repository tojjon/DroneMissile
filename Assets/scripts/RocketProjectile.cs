using UnityEngine;

public class RocketProjectile : MonoBehaviour
{
    public float speed = 30f;
    public float lifeTime = 5f;

    [Tooltip("The object that fired this rocket. Set by Shoting.Fire() right after Instantiate.")]
    public Transform owner;

    private Rigidbody rb;

    // Awake, not Start: Awake runs synchronously inside Instantiate, so no physics step can ever
    // observe this rocket as a non-trigger body. The rocket spawns partly inside the drone's own
    // collider, kinematic-vs-*dynamic* does raise OnCollisionEnter, and the drone now reloads the
    // scene on any collision - a one-step window here would reset the run on every shot.
    void Awake()
    {
        // Hits are detected through trigger events, not OnCollisionEnter: a kinematic Rigidbody
        // generates no collisions against the turret's static colliders. See docs/decisions.md #8.
        // The prefab's Continuous CCD is a no-op on a kinematic body - do not rely on it.
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
    }

    void FixedUpdate()
    {
        // FixedUpdate, not Update: the 0.6 m step is shorter than the collider (0.986 m along Z),
        // so successive test positions overlap and nothing can slip through. In Update() this
        // would depend on frame rate. Holds up to speed ~49 m/s at a 0.02 fixed timestep.
        rb.MovePosition(rb.position + transform.forward * speed * Time.fixedDeltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        // Own shooter - IsChildOf is true for owner itself, so this covers its whole hierarchy.
        if (owner != null && other.transform.IsChildOf(owner)) return;

        Debug.Log("Rocket hit: " + other.gameObject.name);

        // GetComponentInParent, not GetComponent: EnemyTurret sits on the empty parent while the
        // colliders are on its children. See docs/decisions.md #4.
        EnemyTurret turret = other.GetComponentInParent<EnemyTurret>();
        if (turret != null)
        {
            turret.TakeDamage(10);
        }

        Destroy(gameObject);
    }
}
