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
}
