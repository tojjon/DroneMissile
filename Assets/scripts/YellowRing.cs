using UnityEngine;

/// <summary>
/// A yellow hoop. Flying through it adds +1 damage to the drone's next shot, stacking
/// (docs/design/waves.md). Spawned by RunManager, a few per wave.
///
/// The hoop itself has NO colliders: the drone crashes on any solid contact (docs/decisions.md #10),
/// and clipping a ring's edge must not end the run. Only a thin trigger box across the opening
/// detects the pass. Built in code, like the rest of the project's runtime objects.
/// </summary>
public class YellowRing : MonoBehaviour
{
    public float radius = 4f;
    public float thickness = 0.35f;
    public int segments = 20;

    [Tooltip("Set by RunManager. Shared - assigning it to every segment allocates nothing.")]
    public Material material;

    [Tooltip("HDR colour of the pickup flash.")]
    public Color flashColor = new Color(4f, 3.2f, 0.3f, 1f);

    void Start()
    {
        float chord = 2f * Mathf.PI * radius / segments * 1.1f; // slight overlap, no gaps

        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(seg.GetComponent<Collider>());        // see the class comment
            seg.name = "Segment";
            seg.transform.SetParent(transform, false);
            seg.transform.localPosition = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
            seg.transform.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg);
            seg.transform.localScale = new Vector3(thickness, chord, thickness);
            if (material != null) seg.GetComponent<Renderer>().sharedMaterial = material;
        }

        // The ring lies in its local XY plane, so the drone passes along local Z.
        BoxCollider gate = gameObject.AddComponent<BoxCollider>();
        gate.isTrigger = true;
        gate.size = new Vector3(radius * 1.6f, radius * 1.6f, 1f);
    }

    void OnTriggerEnter(Collider other)
    {
        Shoting gun = other.GetComponentInParent<Shoting>();
        if (gun == null) return;   // a projectile, not the drone

        gun.AddBonus(1);

        GameObject fx = new GameObject("RingPickup");
        fx.transform.SetPositionAndRotation(transform.position, transform.rotation);
        ImpactExplosion boom = fx.AddComponent<ImpactExplosion>();
        boom.coreColor = flashColor;
        boom.sparkColor = new Color(flashColor.r * 0.75f, flashColor.g * 0.75f, flashColor.b * 0.75f, flashColor.a);
        boom.scale = 1.5f;

        Destroy(gameObject);
    }
}
