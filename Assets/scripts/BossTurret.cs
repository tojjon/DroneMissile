using UnityEngine;

/// <summary>
/// The wave-10 boss's extra behaviour, next to its EnemyTurret (docs/design/waves.md). From half
/// health it lobs a volley of coloured balls into the air, then another every `volleyInterval`
/// seconds while it lives; each ball spawns a turret of its colour where it lands. Polls
/// EnemyTurret.HealthFraction like the health bars do - no event bus.
///
/// Its health bar is on screen (RunUI), not above it - the exception to docs/decisions.md #13.
/// </summary>
[RequireComponent(typeof(EnemyTurret))]
public class BossTurret : MonoBehaviour
{
    [Header("Colour Balls")]
    [Tooltip("Balls per volley.")]
    public int ballCount = 6;

    [Tooltip("Seconds between volleys once the first has gone.")]
    public float volleyInterval = 25f;

    [Range(0f, 1f)]
    [Tooltip("Health fraction at which the first volley goes.")]
    public float triggerFraction = 0.5f;

    public float launchSpeed = 35f;

    [Tooltip("Max tilt from straight up, in degrees. Wider spreads the turrets further.")]
    public float spreadAngle = 40f;

    [Tooltip("Turrets a ball can spawn, one picked at random per ball. Grey, blue, red, green.")]
    public GameObject[] turretPrefabs;

    [Tooltip("Ball colour per entry in turretPrefabs, same order.")]
    public Material[] ballMaterials;

    public float ballSize = 1.5f;

    [Tooltip("Colours the balls may be, set per wave by RunManager (waves 20 and 30 narrow it). None = all four.")]
    public RunManager.BallColours ballColours = RunManager.BallColours.None;

    [Tooltip("Where the balls leave from, above the boss's root.")]
    public float launchHeight = 8f;

    private EnemyTurret turret;
    private float nextVolley = float.PositiveInfinity;
    private bool triggered;

    void Awake()
    {
        turret = GetComponent<EnemyTurret>();
    }

    void Update()
    {
        if (!triggered && turret.HealthFraction <= triggerFraction)
        {
            triggered = true;
            nextVolley = Time.time;
        }

        // Frozen (Freeze upgrade): the volley timer stands still with the rest of the turret.
        if (turret.IsFrozen)
        {
            nextVolley += Time.deltaTime;
            return;
        }

        if (Time.time >= nextVolley)
        {
            Volley();
            nextVolley = Time.time + volleyInterval;
        }
    }

    // A random index into turretPrefabs among the allowed colours (bit i = index i). -1 if none of
    // the allowed colours has a prefab.
    int PickKind()
    {
        int allowed = (int)ballColours;
        int count = 0;
        for (int i = 0; i < turretPrefabs.Length; i++)
        {
            if (Allowed(allowed, i)) count++;
        }
        if (count == 0) return -1;

        int pick = Random.Range(0, count);
        for (int i = 0; i < turretPrefabs.Length; i++)
        {
            if (!Allowed(allowed, i)) continue;
            if (pick-- == 0) return i;
        }
        return -1;
    }

    bool Allowed(int mask, int i)
    {
        return turretPrefabs[i] != null && (mask == 0 || (mask & (1 << i)) != 0);
    }

    void Volley()
    {
        if (turretPrefabs == null || turretPrefabs.Length == 0) return;

        Vector3 origin = transform.position + Vector3.up * launchHeight;
        for (int i = 0; i < ballCount; i++)
        {
            int kind = PickKind();
            if (kind < 0) return;

            // Evenly round the compass, tilted a random amount off vertical, so balls spread out
            // instead of piling onto one spot.
            float yaw = (i + Random.value * 0.5f) * 360f / ballCount;
            float tilt = Random.Range(spreadAngle * 0.4f, spreadAngle);
            Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(tilt, 0f, 0f) * Vector3.up;

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());   // see ColorBall - nothing may collide with it
            go.name = "ColorBall";
            go.transform.position = origin;
            go.transform.localScale = Vector3.one * ballSize;
            if (ballMaterials != null && kind < ballMaterials.Length && ballMaterials[kind] != null)
            {
                go.GetComponent<Renderer>().sharedMaterial = ballMaterials[kind];
            }

            ColorBall ball = go.AddComponent<ColorBall>();
            ball.turretPrefab = turretPrefabs[kind];
            ball.owner = transform;
            ball.velocity = dir * launchSpeed;
        }
    }
}
