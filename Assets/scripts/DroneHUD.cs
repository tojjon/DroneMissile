using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The whole HUD: a red border while the drone is stunned, and a death message during the beat
/// between a crash and the scene reload. Both are driven by polling DroneControls once a frame -
/// this project has no event bus and adding one for two booleans would not pay for itself.
///
/// The canvas is built here in code instead of being authored into SampleScene.unity: scenes are
/// not hand-edited as YAML in this repo (CLAUDE.md), the vignette is a generated texture that would
/// have to be produced at runtime anyway, and keeping the hierarchy in C# means the whole feature
/// is one reviewable file rather than an opaque scene diff. See docs/decisions.md #12.
/// </summary>
public class DroneHUD : MonoBehaviour
{
    [Header("Stun Border")]
    public Color stunColor = new Color(1f, 0.1f, 0.1f, 0.75f);

    [Tooltip("How far the glow reaches inward, as a fraction of the half-screen. 0.5 fills the screen.")]
    [Range(0.05f, 0.5f)]
    public float borderThickness = 0.35f;

    [Tooltip("Alpha per second while ramping. In fast, out slow, so the border neither pops nor lingers.")]
    public float fadeInSpeed = 6f;
    public float fadeOutSpeed = 2.5f;

    [Tooltip("Alarm pulse while stunned: cycles per second, and how much of the alpha it swings.")]
    public float pulseSpeed = 3f;
    [Range(0f, 1f)]
    public float pulseDepth = 0.35f;

    [Header("Death")]
    public string deathMessage = "YOU DIED";
    public Color deathColor = new Color(1f, 0.15f, 0.15f, 1f);
    public int deathFontSize = 96;

    [Tooltip("Fade-in of the death message. Keep it well under DroneControls.deathDelay or it never lands.")]
    public float deathFadeDuration = 0.25f;

    // Sizes are in reference-resolution units (1920x1080), so the CanvasScaler scales them with the
    // window. Screen centre is the true aim point - see the note on BuildCrosshair().
    [Header("Crosshair")]
    public Color crosshairColor = new Color(1f, 1f, 1f, 0.85f);

    [Tooltip("Half-width of the open centre. The gap is what keeps the target visible.")]
    public float crosshairGap = 6f;
    public float crosshairLength = 14f;
    public float crosshairThickness = 2f;

    [Tooltip("Dark edge on each tick. Without it the cross vanishes against bright sky.")]
    public bool crosshairOutline = true;
    public Color crosshairOutlineColor = new Color(0f, 0f, 0f, 0.6f);

    private DroneControls drone;
    private Image vignette;
    private Text deathText;
    private CanvasGroup crosshair;

    private float vignetteAlpha;
    private float deathAlpha;

    void Start()
    {
        // Tag contract, same as EnemyTurret targeting: the drone root carries the Player tag.
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) drone = player.GetComponent<DroneControls>();

        if (drone == null)
        {
            // Warn rather than throw - the scene is still playable without a HUD.
            Debug.LogWarning("DroneHUD: no DroneControls on a GameObject tagged Player. HUD disabled.");
            enabled = false;
            return;
        }

        Build();
    }

    void Update()
    {
        // IsStunned goes false the moment the drone crashes, so the border fades out under the
        // death message instead of sitting there at full red.
        float target = drone.IsStunned ? stunColor.a * Pulse() : 0f;
        float speed = target > vignetteAlpha ? fadeInSpeed : fadeOutSpeed;

        vignetteAlpha = Mathf.MoveTowards(vignetteAlpha, target, speed * Time.deltaTime);
        vignette.color = WithAlpha(stunColor, vignetteAlpha);

        if (drone.HasCrashed && deathAlpha < 1f)
        {
            deathAlpha = deathFadeDuration > 0f
                ? Mathf.MoveTowards(deathAlpha, 1f, Time.deltaTime / deathFadeDuration)
                : 1f;
            deathText.color = WithAlpha(deathColor, deathAlpha * deathColor.a);

            // Ride the same curve rather than running a second timer: the cross goes out exactly as
            // the death message comes in. Nothing left to keep in sync, and no extra tunable.
            crosshair.alpha = 1f - deathAlpha;
        }
    }

    // Swings between (1 - pulseDepth) and 1, starting at 1, so the pulse only ever dims the border
    // below the configured alpha - it never overshoots it.
    float Pulse()
    {
        return 1f - pulseDepth * 0.5f * (1f - Mathf.Cos(Time.time * pulseSpeed * 2f * Mathf.PI));
    }

    void Build()
    {
        // Screen space overlay, so it is unaffected by Main Camera being a child of the drone.
        // No GraphicRaycaster and no EventSystem: this is display only and never takes input.
        GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // The sprite is white with the shape in its alpha channel, so stunColor tints it live.
        vignette = NewGraphic<Image>(canvasGo.transform, "StunVignette");
        vignette.sprite = BuildVignetteSprite();
        vignette.type = Image.Type.Simple;
        vignette.color = WithAlpha(stunColor, 0f);

        // Sibling order is draw order. The cross goes above the vignette (which is edge-only, so
        // they never overlap anyway) and below the death message, which must win the centre.
        crosshair = BuildCrosshair(canvasGo.transform);

        // Added after the vignette, so it is a later sibling and draws on top of it.
        deathText = NewGraphic<Text>(canvasGo.transform, "DeathText");

        // Arial.ttf was removed after 2022.2; LegacyRuntime.ttf is the builtin font in Unity 6.
        // Using it keeps TMP Essential Resources (~2 MB of assets) out of the repo.
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) Debug.LogWarning("DroneHUD: builtin font LegacyRuntime.ttf not found; the death message will not render.");

        deathText.font = font;
        deathText.text = deathMessage;
        deathText.fontSize = deathFontSize;
        deathText.fontStyle = FontStyle.Bold;
        deathText.alignment = TextAnchor.MiddleCenter;
        deathText.horizontalOverflow = HorizontalWrapMode.Overflow;
        deathText.verticalOverflow = VerticalWrapMode.Overflow;
        deathText.color = WithAlpha(deathColor, 0f);
    }

    Sprite BuildVignetteSprite()
    {
        // Sized to the screen aspect and measured against the shorter axis, so the band comes out
        // the same thickness on all four edges. One square texture stretched to 16:9 would give
        // visibly fatter left and right bands than top and bottom.
        const int LongSide = 128;
        int width = LongSide;
        int height = LongSide;

        if (Screen.width >= Screen.height)
            height = Mathf.Max(8, Mathf.RoundToInt(LongSide * (float)Screen.height / Screen.width));
        else
            width = Mathf.Max(8, Mathf.RoundToInt(LongSide * (float)Screen.width / Screen.height));

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear; // this is what turns 128 texels into a soft glow
        tex.wrapMode = TextureWrapMode.Clamp;

        float shortHalf = Mathf.Min(width, height) * 0.5f;
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Distance to the nearest edge, normalised by the shorter half-extent.
                float dx = Mathf.Min(x + 0.5f, width - 0.5f - x);
                float dy = Mathf.Min(y + 0.5f, height - 0.5f - y);
                float edge = Mathf.Min(dx, dy) / shortHalf;

                float alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(edge / borderThickness));
                pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f));
    }

    // A static cross at screen centre is the *correct* aim marker here, not an approximation:
    // Main Camera and FirePoint are siblings under the drone with bit-identical local rotations
    // (both -25 deg pitch), so the muzzle axis and the optical axis are exactly parallel - 0.000 deg
    // apart - with zero lateral offset. The muzzle sits 4.15 mm below the optical axis, which is a
    // 0.024 deg error at 10 m and less further out: under a tenth of a pixel at 1080p. Do not
    // "fix" this with a raycast or a world-space marker. See docs/decisions.md #12.
    CanvasGroup BuildCrosshair(Transform parent)
    {
        GameObject root = new GameObject("Crosshair", typeof(RectTransform), typeof(CanvasGroup));
        root.transform.SetParent(parent, false);

        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;

        // One float drives all four ticks when the drone dies.
        CanvasGroup group = root.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        // Distance from centre to the middle of a tick: past the gap, then half the tick's length.
        float offset = crosshairGap + crosshairLength * 0.5f;
        Vector2 vertical = new Vector2(crosshairThickness, crosshairLength);
        Vector2 horizontal = new Vector2(crosshairLength, crosshairThickness);

        NewTick(root.transform, "Up", vertical, new Vector2(0f, offset));
        NewTick(root.transform, "Down", vertical, new Vector2(0f, -offset));
        NewTick(root.transform, "Left", horizontal, new Vector2(-offset, 0f));
        NewTick(root.transform, "Right", horizontal, new Vector2(offset, 0f));

        return group;
    }

    // One tick. An Image with no sprite draws a solid quad, so unlike the vignette the cross needs
    // no generated texture at all.
    void NewTick(Transform parent, string name, Vector2 size, Vector2 position)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = position;

        Image image = go.AddComponent<Image>();
        image.color = crosshairColor;
        image.raycastTarget = false;

        if (crosshairOutline)
        {
            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = crosshairOutlineColor;
            outline.effectDistance = new Vector2(1f, -1f);
        }
    }

    // A full-screen stretched graphic. Nothing in this project raycasts UI, so raycastTarget is off
    // on everything - it also means the HUD can never swallow a click.
    static T NewGraphic<T>(Transform parent, string name) where T : Graphic
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        T graphic = go.AddComponent<T>();
        graphic.raycastTarget = false;
        return graphic;
    }

    static Color WithAlpha(Color c, float a)
    {
        return new Color(c.r, c.g, c.b, a);
    }
}
