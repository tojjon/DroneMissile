using UnityEngine;

/// <summary>
/// A coloured ball the boss lobs into the air (docs/design/waves.md). Ballistic: its own gravity,
/// and a raycast sweep each step so it cannot fall through the floor. Where it lands on an
/// up-facing surface it spawns a turret of its colour and registers it with RunManager, so the
/// wave is not cleared until that turret is dead too. Off a wall or the ceiling it bounces.
///
/// No Rigidbody and no collider of its own: nothing needs to hit it, and the drone flying into it
/// must not count as a crash.
/// </summary>
public class ColorBall : MonoBehaviour
{
    [Tooltip("Spawned where the ball lands. Set by BossTurret.")]
    public GameObject turretPrefab;

    [Tooltip("The boss - the sweep ignores its hierarchy, since the ball spawns inside it.")]
    public Transform owner;

    public Vector3 velocity;
    public float maxLife = 20f;

    [Tooltip("A surface counts as ground when its normal is at least this close to straight up.")]
    public float groundDot = 0.7f;

    [Tooltip("Speed kept after bouncing off a wall or the ceiling.")]
    public float bounce = 0.6f;

    static readonly RaycastHit[] Buffer = new RaycastHit[8];
    private float dieAt;

    // Balls still in the air. RunManager does not call a wave cleared while any are flying - the
    // boss can die mid-volley, and the turrets those balls will spawn still have to be beaten.
    public static int InFlight { get; private set; }

    void Awake()
    {
        InFlight++;
    }

    void OnDestroy()
    {
        InFlight--;
    }

    void Start()
    {
        dieAt = Time.time + maxLife;
    }

    void FixedUpdate()
    {
        if (Time.time > dieAt) { Destroy(gameObject); return; }

        velocity += Physics.gravity * Time.fixedDeltaTime;
        Vector3 from = transform.position;
        Vector3 step = velocity * Time.fixedDeltaTime;

        if (Nearest(from, step, out RaycastHit hit))
        {
            if (Vector3.Dot(hit.normal, Vector3.up) >= groundDot)
            {
                Land(hit.point);
                return;
            }

            velocity = Vector3.Reflect(velocity, hit.normal) * bounce;
            transform.position = hit.point + hit.normal * 0.2f;
            return;
        }

        transform.position = from + step;
    }

    bool Nearest(Vector3 from, Vector3 step, out RaycastHit nearest)
    {
        nearest = default;
        float distance = step.magnitude;
        if (distance <= 0f) return false;

        int count = Physics.RaycastNonAlloc(from, step / distance, Buffer, distance, ~0, QueryTriggerInteraction.Ignore);
        float best = float.PositiveInfinity;
        bool found = false;
        for (int i = 0; i < count; i++)
        {
            if (owner != null && Buffer[i].transform.IsChildOf(owner)) continue;
            if (Buffer[i].distance >= best) continue;
            best = Buffer[i].distance;
            nearest = Buffer[i];
            found = true;
        }
        return found;
    }

    void Land(Vector3 point)
    {
        if (turretPrefab != null)
        {
            GameObject t = Instantiate(turretPrefab, point, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            RunManager.RestOnGround(t, point.y);

            RunManager run = RunManager.Instance;
            if (run != null) run.Register(t.GetComponent<EnemyTurret>());
        }

        Destroy(gameObject);
    }
}
