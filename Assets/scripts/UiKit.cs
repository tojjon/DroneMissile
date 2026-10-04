using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Look and sizes shared by the menus. A plain class so each screen can expose its own copy in the
/// Inspector.
/// </summary>
[System.Serializable]
public class UiStyle
{
    public int titleFontSize = 84;
    public int buttonFontSize = 34;
    public Vector2 buttonSize = new Vector2(420f, 68f);
    public float buttonSpacing = 14f;

    public Color titleColor = Color.white;
    public Color panelColor = new Color(0f, 0f, 0f, 0.55f);
    public Color buttonColor = new Color(0.12f, 0.12f, 0.14f, 0.9f);
    public Color buttonHighlightColor = new Color(0.95f, 0.45f, 0.1f, 1f);
    public Color buttonTextColor = Color.white;
    public Color disabledTextColor = new Color(1f, 1f, 1f, 0.3f);

    [System.NonSerialized] public Font font;
}

/// <summary>
/// Builders for the code-built menus: MainMenu and RunUI (docs/decisions.md #21, #23). Same
/// conventions as DroneHUD - legacy Text with the builtin LegacyRuntime.ttf, spriteless Images, a
/// 1920x1080 CanvasScaler - plus what the HUD deliberately lacks, because menus take input: a
/// GraphicRaycaster and an EventSystem with InputSystemUIInputModule.
/// </summary>
public static class UiKit
{
    public static Font BuiltinFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) Debug.LogWarning("UiKit: builtin font LegacyRuntime.ttf not found; menu text will not render.");
        return font;
    }

    // The project runs the new Input System only, so the legacy StandaloneInputModule would throw.
    public static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;

        GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        // Explicit, so arrows/Enter/mouse work without an actions asset wired in the Inspector.
        go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    public static GameObject NewCanvas(Transform parent, int sortingOrder)
    {
        GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(parent, false);

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        // Same scaling as DroneHUD, so sizes are in 1920x1080 units.
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvasGo;
    }

    public static T NewRect<T>(Transform parent, string name) where T : Graphic
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.AddComponent<T>();
    }

    // A full-rect graphic stretched over its parent.
    public static T NewFill<T>(Transform parent, string name) where T : Graphic
    {
        T g = NewRect<T>(parent, name);
        RectTransform rt = g.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        g.raycastTarget = false;
        return g;
    }

    public static Text NewText(Transform parent, string name, string content, int size, Color color, Font font)
    {
        Text text = NewRect<Text>(parent, name);
        text.font = font;
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.raycastTarget = false; // labels must never swallow a click meant for their button
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    // A label sized like a button, for use inside a column.
    public static Text NewLabel(Transform parent, string content, UiStyle s)
    {
        Text text = NewText(parent, content, content, s.buttonFontSize, s.titleColor, s.font);
        text.alignment = TextAnchor.MiddleLeft;
        text.rectTransform.sizeDelta = s.buttonSize;
        return text;
    }

    // A vertical stack, anchored and positioned by the caller.
    public static GameObject NewColumn(Transform parent, string name, Vector2 anchor, Vector2 pivot,
                                       Vector2 position, float width, float spacing, TextAnchor align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(width, 600f);

        VerticalLayoutGroup layout = go.GetComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = align;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return go;
    }

    public static Button NewButton(Transform parent, string label, UnityAction onClick, UiStyle s)
    {
        GameObject go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().sizeDelta = s.buttonSize;

        // A spriteless Image is a solid quad - no texture assets, same as the HUD's crosshair.
        Image image = go.GetComponent<Image>();
        image.color = Color.white;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;

        // Tints multiply the white quad. Selected matters as much as Highlighted: keyboard
        // navigation only ever selects, it never hovers.
        ColorBlock colors = button.colors;
        colors.normalColor = s.buttonColor;
        colors.highlightedColor = s.buttonHighlightColor;
        colors.selectedColor = s.buttonHighlightColor;
        colors.pressedColor = s.buttonHighlightColor * 0.8f;
        colors.disabledColor = new Color(s.buttonColor.r, s.buttonColor.g, s.buttonColor.b, s.buttonColor.a * 0.5f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        Text text = NewText(go.transform, "Label", label, s.buttonFontSize, s.buttonTextColor, s.font);
        text.alignment = TextAnchor.MiddleLeft;
        RectTransform textRt = text.rectTransform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(24f, 0f);
        textRt.offsetMax = Vector2.zero;
        return button;
    }

    // Button transitions tint only the target graphic, so a disabled button's label would stay
    // bright - grey it out alongside.
    public static void SetInteractable(Button button, bool value, UiStyle s)
    {
        button.interactable = value;
        button.GetComponentInChildren<Text>().color = value ? s.buttonTextColor : s.disabledTextColor;
    }

    // A mouse click on empty space clears the selection, after which arrow keys do nothing. Put it
    // back on `first` so the keyboard always has somewhere to start. Call once a frame.
    public static void KeepSelection(GameObject first)
    {
        EventSystem es = EventSystem.current;
        if (es == null || first == null || !first.activeInHierarchy) return;
        if (es.currentSelectedGameObject == null || !es.currentSelectedGameObject.activeInHierarchy)
        {
            es.SetSelectedGameObject(first);
        }
    }

    public static void Select(GameObject go)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(go);
    }

    // ---- upgrade cards (docs/plans/upgrades.md) ---------------------------------------------

    // Viktor's card art is 71 x 100 pixel art; scale by whole numbers so the pixels stay square.
    public const float CardW = 71f, CardH = 100f;

    public static Image NewCardArt(Transform parent, Sprite sprite, float scale)
    {
        Image img = NewRect<Image>(parent, "Art");
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        img.rectTransform.sizeDelta = new Vector2(CardW * scale, CardH * scale);
        return img;
    }

    // A pickable card: the art with the upgrade's name under it, on a frame that lights up in the
    // style's highlight colour when hovered or selected with the keyboard.
    public static Button NewCardButton(Transform parent, UpgradeDefinition def, float scale, UnityAction onClick, UiStyle s)
    {
        const float pad = 12f, nameH = 52f;
        Vector2 art = new Vector2(CardW * scale, CardH * scale);

        GameObject go = new GameObject(def.displayName, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(art.x + 2f * pad, art.y + 2f * pad + nameH);

        Image frame = go.GetComponent<Image>();
        frame.color = Color.white;
        Button button = go.GetComponent<Button>();
        button.targetGraphic = frame;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0.08f);
        colors.highlightedColor = s.buttonHighlightColor;
        colors.selectedColor = s.buttonHighlightColor;
        colors.pressedColor = s.buttonHighlightColor * 0.8f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        RectTransform artRt = NewCardArt(go.transform, def.card, scale).rectTransform;
        artRt.anchorMin = artRt.anchorMax = artRt.pivot = new Vector2(0.5f, 1f);
        artRt.anchoredPosition = new Vector2(0f, -pad);

        Text name = NewText(go.transform, "Name", def.displayName, 28, s.buttonTextColor, s.font);
        name.alignment = TextAnchor.MiddleCenter;
        RectTransform nrt = name.rectTransform;
        nrt.anchorMin = new Vector2(0f, 0f);
        nrt.anchorMax = new Vector2(1f, 0f);
        nrt.pivot = new Vector2(0.5f, 0f);
        nrt.sizeDelta = new Vector2(0f, nameH);
        nrt.anchoredPosition = new Vector2(0f, pad * 0.5f);
        return button;
    }

    public static HorizontalLayoutGroup NewRow(Transform parent, string name, Vector2 size, float spacing)
    {
        GameObject row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);
        row.GetComponent<RectTransform>().sizeDelta = size;
        HorizontalLayoutGroup h = row.GetComponent<HorizontalLayoutGroup>();
        h.spacing = spacing;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = h.childControlHeight = false;
        h.childForceExpandWidth = h.childForceExpandHeight = false;
        return h;
    }

    // Children of a rebuilt container go away now, not at the end of the frame - otherwise the
    // layout counts them once more and the new row jumps.
    public static void Clear(Transform container)
    {
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            GameObject child = container.GetChild(i).gameObject;
            child.SetActive(false);
            Object.Destroy(child);
        }
    }
}
