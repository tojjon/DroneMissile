using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A health bar floating above the turret, built in code and polled once a frame - same pattern as
/// DroneHUD, for the same reasons: scenes are not hand-edited as YAML in this repo (CLAUDE.md), and
/// keeping the hierarchy in C# means the whole feature is one reviewable file instead of an opaque
/// scene diff. Unlike DroneHUD this canvas is *world space*: the bar belongs to the turret, so it
/// has to be occluded by terrain, shrink with distance and sit over the right turret when there is
/// more than one. See docs/decisions.md #13.
///
/// Lives on the same GameObject as EnemyTurret - the empty `turret` parent, which never rotates.
/// Turret_Barrel does, and a bar parented to that would swing around the head.
/// </summary>
[RequireComponent(typeof(EnemyTurret))]
public class TurretHealthBar : MonoBehaviour
{
    [Header("Placement")]
    [Tooltip("Metres above the turret root. Turret_Head sits at y 1.93 with a 1.6 m sphere on it, so anything below ~2.8 clips into the turret.")]
    public float heightOffset = 3.2f;

    [Tooltip("Bar size in metres. This is world space, so the bar shrinks with distance like any other object.")]
    public float width = 1.8f;
    public float height = 0.22f;

    [Tooltip("Dark frame left visible around the fill, in metres per side.")]
    public float padding = 0.03f;

    [Header("Visibility")]
    [Tooltip("Hide the bar past this distance from the camera. Keep it above EnemyTurret.detectionRange, or a turret can shoot from behind an invisible bar.")]
    public float visibleRange = 90f;

    [Tooltip("Show the bar only after the turret has taken its first hit.")]
    public bool hideWhenUndamaged = false;

    [Header("Colours")]
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.65f);
    public Color fullColor = new Color(0.25f, 0.9f, 0.3f, 1f);
    public Color midColor = new Color(0.95f, 0.8f, 0.15f, 1f);
    public Color lowColor = new Color(0.9f, 0.15f, 0.15f, 1f);

    [Tooltip("How fast the fill chases the real value, in bar-fractions per second. 0 snaps instantly.")]
    public float drainSpeed = 1.5f;

    private EnemyTurret turret;
    private Camera view;
    private Transform bar;          // canvas root; billboarded every frame
    private RectTransform fill;
    private Image fillImage;
    private float displayed = 1f;

    void Start()
    {
        turret = GetComponent<EnemyTurret>();

        // The drone's Main Camera carries the MainCamera tag. Warn rather than throw - the turret
        // is perfectly playable without a bar over it.
        view = Camera.main;
        if (view == null)
        {
            Debug.LogWarning("TurretHealthBar: no camera tagged MainCamera. Health bar disabled.");
            enabled = false;
            return;
        }

        // EnemyTurret fills currentHealth in Awake(), so this is the real value even though Start()
        // order between the two components is undefined.
        displayed = turret.HealthFraction;

        Build();
    }

    // LateUpdate, not Update: the billboard has to be applied after everything that could have moved
    // the camera this frame, or the bar lags the view by a frame while the drone rolls.
    void LateUpdate()
    {
        float target = turret.HealthFraction;

        displayed = drainSpeed > 0f
            ? Mathf.MoveTowards(displayed, target, drainSpeed * Time.deltaTime)
            : target;

        // Distance is measured to the bar, not the turret root, which is what the player is looking
        // at. Undamaged turrets can be hidden entirely - the flag also waits out the drain, so the
        // bar does not vanish mid-animation when the last hit lands.
        bool visible = Vector3.Distance(view.transform.position, bar.position) <= visibleRange
                       && (!hideWhenUndamaged || target < 1f || displayed < 1f);

        if (bar.gameObject.activeSelf != visible) bar.gameObject.SetActive(visible);
        if (!visible) return;

        // Anchor-driven, not Image.Type.Filled: a filled Image needs a sprite, and every graphic in
        // this project is a spriteless quad. The fill sits inside a padded FillArea, so shrinking
        // its anchor can never eat into the frame.
        fill.anchorMax = new Vector2(Mathf.Clamp01(displayed), 1f);
        fillImage.color = Tint(displayed);

        // Copy the camera's rotation rather than LookAt: that keeps the bar parallel to the screen
        // plane, so it stays a rectangle instead of shearing when the turret is off to one side.
        bar.rotation = view.transform.rotation;
    }

    // A straight red-to-green lerp runs through mud in the middle, so it goes through an explicit
    // yellow at half health.
    Color Tint(float fraction)
    {
        float f = Mathf.Clamp01(fraction);
        return f < 0.5f
            ? Color.Lerp(lowColor, midColor, f * 2f)
            : Color.Lerp(midColor, fullColor, (f - 0.5f) * 2f);
    }

    void Build()
    {
        // World-space canvas: sizeDelta is in metres because the turret root has scale 1. No
        // GraphicRaycaster and no worldCamera - this is display only and never takes input.
        GameObject canvasGo = new GameObject("HealthBar", typeof(RectTransform), typeof(Canvas));
        canvasGo.transform.SetParent(transform, false);

        bar = canvasGo.transform;
        bar.localPosition = new Vector3(0f, heightOffset, 0f);

        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;

        RectTransform barRect = (RectTransform)bar;
        barRect.sizeDelta = new Vector2(width, height);

        NewImage(bar, "Background", backgroundColor);

        // The padded inner rect. The fill anchors inside *this*, which is what keeps the frame
        // intact at low health.
        GameObject area = new GameObject("FillArea", typeof(RectTransform));
        area.transform.SetParent(bar, false);

        RectTransform areaRect = (RectTransform)area.transform;
        Stretch(areaRect);
        areaRect.offsetMin = new Vector2(padding, padding);
        areaRect.offsetMax = new Vector2(-padding, -padding);

        fillImage = NewImage(area.transform, "Fill", Tint(displayed));
        fill = fillImage.rectTransform;
        fill.anchorMin = new Vector2(0f, 0f);
        fill.anchorMax = new Vector2(Mathf.Clamp01(displayed), 1f);
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        fill.pivot = new Vector2(0f, 0.5f); // drains toward the left edge
    }

    // An Image with no sprite draws a solid quad, so the bar needs no texture at all.
    static Image NewImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        Stretch((RectTransform)go.transform);

        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
