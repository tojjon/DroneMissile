using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The sandbox's upgrade console (docs/plans/sandbox-console.md): Enter opens a text field, typing
/// an upgrade's name gives it to the drone. `remove name` takes one stack off, `clear` takes all.
/// Names match by id or display name, ignoring case, spaces and underscores, and a unique start is
/// enough ("dou" = Double Strike). Matching upgrades are listed under the field as you type.
///
/// The game is paused (timeScale 0) while the field is open, so WASD and Space type instead of
/// flying. Owned upgrades live in GameSession, so they survive a crash reload and are cleared only
/// by the main menu. Built in code with UiKit, like RunUI.
/// </summary>
public class SandboxConsole : MonoBehaviour
{
    public UiStyle style = new UiStyle();

    [Tooltip("How long the result of a command stays on screen after the console closes.")]
    public float messageSeconds = 3f;

    public Color errorColor = new Color(1f, 0.35f, 0.3f, 1f);
    public Color okColor = new Color(0.5f, 1f, 0.5f, 1f);
    public Color hintColor = new Color(1f, 1f, 1f, 0.6f);

    // ReturnToMenu asks this, so the Esc that closes the console does not also leave the sandbox.
    // Also true on the frame the console closed: the two components update in undefined order.
    public static bool HoldsEscape => isOpen || closedFrame == Time.frameCount;

    static bool isOpen;
    static int closedFrame = -1;

    private GameObject panel;
    private InputField field;
    private Text suggestions;
    private Text message;
    private Text owned;
    private Text hint;
    private float messageUntil;
    private float pausedTimeScale = 1f;

    void Start()
    {
        style.font = UiKit.BuiltinFont();
        UiKit.EnsureEventSystem();
        Build();
        SetOpen(false);
        RefreshOwned();
    }

    void OnDestroy()
    {
        if (isOpen) Time.timeScale = pausedTimeScale;
        isOpen = false;
    }

    void Update()
    {
        // Closing happens inside the field's callback, during the EventSystem's update - the same
        // Enter press must not reopen it, whichever of us runs first this frame.
        if (!isOpen && Time.frameCount > closedFrame + 1 && EnterPressed()) SetOpen(true);

        bool showMessage = isOpen || Time.unscaledTime < messageUntil;
        message.gameObject.SetActive(showMessage && message.text.Length > 0);
    }

    static bool EnterPressed()
    {
        Keyboard k = Keyboard.current;
        return k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame);
    }

    void SetOpen(bool open)
    {
        if (open == isOpen && panel.activeSelf == open) return;

        if (open)
        {
            pausedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
        else if (isOpen)
        {
            Time.timeScale = pausedTimeScale;
            closedFrame = Time.frameCount;
        }
        isOpen = open;

        panel.SetActive(open);
        hint.gameObject.SetActive(!open);
        owned.gameObject.SetActive(!open);   // the suggestions show owned stacks instead

        if (open)
        {
            message.text = "";
            field.text = "";
            RefreshSuggestions("");
            UiKit.Select(field.gameObject);
            field.ActivateInputField();
        }
        else
        {
            // Left selected, the field would take the UI module's Submit for this same Enter and
            // activate itself again.
            UiKit.Select(null);
            messageUntil = Time.unscaledTime + messageSeconds;
        }
    }

    // Enter, Esc or a click elsewhere. Esc puts back the text the field opened with, which is
    // always empty - so empty means close, anything else is a command.
    void OnEndEdit(string text)
    {
        if (!isOpen) return;
        if (text.Trim().Length == 0)
        {
            SetOpen(false);
            return;
        }

        if (Execute(text.Trim()))
        {
            RefreshOwned();
            SetOpen(false);
        }
        else
        {
            field.ActivateInputField();
        }
    }

    // ---- commands ---------------------------------------------------------------------------

    bool Execute(string command)
    {
        if (Normalize(command) == "clear")
        {
            GameSession.ClearUpgrades();
            return Report("All upgrades removed.", true);
        }

        bool remove = IsRemove(command, out string name);
        List<UpgradeDefinition> matches = Match(name);

        if (matches.Count == 0) return Report("Unknown upgrade \"" + name + "\".", false);
        if (matches.Count > 1) return Report("Which one? " + JoinNames(matches), false);

        UpgradeDefinition def = matches[0];
        if (remove)
        {
            if (!GameSession.RemoveUpgrade(def.id)) return Report(def.displayName + " is not owned.", false);
            return Report("- " + def.displayName + StackSuffix(def.id), true);
        }

        GameSession.AddUpgrade(def.id, sandbox: true);
        return Report("+ " + def.displayName + StackSuffix(def.id), true);
    }

    bool Report(string text, bool ok)
    {
        message.text = text;
        message.color = ok ? okColor : errorColor;
        return ok;
    }

    static bool IsRemove(string command, out string rest)
    {
        const string prefix = "remove ";
        if (command.Length > prefix.Length && command.ToLowerInvariant().StartsWith(prefix))
        {
            rest = command.Substring(prefix.Length).Trim();
            return true;
        }
        rest = command;
        return false;
    }

    // An exact id or name wins; otherwise every upgrade whose id or name starts with the query.
    static List<UpgradeDefinition> Match(string query)
    {
        string q = Normalize(query);
        List<UpgradeDefinition> found = new List<UpgradeDefinition>();
        foreach (UpgradeDefinition d in UpgradeCatalog.All)
        {
            if (Normalize(d.id) == q || Normalize(d.displayName) == q) return new List<UpgradeDefinition> { d };
            if (Normalize(d.id).StartsWith(q) || Normalize(d.displayName).StartsWith(q)) found.Add(d);
        }
        return found;
    }

    static string Normalize(string s)
    {
        StringBuilder sb = new StringBuilder(s.Length);
        foreach (char c in s)
        {
            if (c != ' ' && c != '_' && c != '-') sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    static string StackSuffix(string id)
    {
        int n = GameSession.Stacks(id);
        return n > 1 ? "  x" + n : "";
    }

    static string JoinNames(List<UpgradeDefinition> defs)
    {
        List<string> names = new List<string>();
        foreach (UpgradeDefinition d in defs) names.Add(d.displayName);
        return string.Join(", ", names);
    }

    // ---- display ----------------------------------------------------------------------------

    void RefreshSuggestions(string text)
    {
        IsRemove(text.Trim(), out string name);
        StringBuilder sb = new StringBuilder();
        foreach (UpgradeDefinition d in Match(name))
        {
            string colour = ColorUtility.ToHtmlStringRGB(Rarities.Colour(d.rarity));
            sb.Append("<color=#").Append(colour).Append('>').Append(d.displayName).Append("</color>");
            int n = GameSession.Stacks(d.id);
            if (n > 0) sb.Append("  (owned x").Append(n).Append(')');
            sb.Append('\n');
        }
        if (sb.Length == 0) sb.Append("no match\n");
        sb.Append("<color=#ffffff88>name = add  ·  remove name  ·  clear  ·  Esc = close</color>");
        suggestions.text = sb.ToString();
    }

    void RefreshOwned()
    {
        List<string> parts = new List<string>();
        foreach (string id in GameSession.OwnedUpgrades)
        {
            UpgradeDefinition d = UpgradeCatalog.Find(id);
            parts.Add((d != null ? d.displayName : id) + StackSuffix(id));
        }
        owned.text = parts.Count > 0 ? string.Join("  ·  ", parts) : "";
    }

    // ---- construction -----------------------------------------------------------------------

    const float Margin = 40f, Width = 680f, FieldHeight = 56f;

    void Build()
    {
        // Above DroneHUD; there is no RunUI in the sandbox.
        Transform canvas = UiKit.NewCanvas(transform, 150).transform;

        hint = Corner(canvas, "Hint", "ENTER  upgrade console", 22, hintColor, Margin);
        owned = Corner(canvas, "Owned", "", 24, Color.white, Margin + 32f);
        message = Corner(canvas, "Message", "", 26, okColor, Margin + FieldHeight + 16f);

        panel = new GameObject("Console", typeof(RectTransform));
        panel.transform.SetParent(canvas, false);
        RectTransform prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = prt.anchorMax = prt.pivot = Vector2.zero;
        prt.anchoredPosition = Vector2.zero;
        prt.sizeDelta = Vector2.zero;
        // Message sits outside the panel so it can outlive it, but must draw above its background.
        panel.transform.SetSiblingIndex(0);

        Image back = UiKit.NewRect<Image>(panel.transform, "Background");
        back.color = style.panelColor;
        back.raycastTarget = false;
        Place(back.rectTransform, Margin - 16f, Margin - 16f, Width + 32f, 360f);

        field = BuildField(panel.transform);
        field.onValueChanged.AddListener(RefreshSuggestions);
        field.onEndEdit.AddListener(OnEndEdit);

        suggestions = UiKit.NewText(panel.transform, "Suggestions", "", 26, Color.white, style.font);
        suggestions.alignment = TextAnchor.LowerLeft;
        suggestions.supportRichText = true;
        Place(suggestions.rectTransform, Margin + 8f, Margin + FieldHeight + 64f, Width, 200f);
    }

    InputField BuildField(Transform parent)
    {
        GameObject go = new GameObject("Field", typeof(RectTransform), typeof(Image), typeof(InputField));
        go.transform.SetParent(parent, false);
        Place(go.GetComponent<RectTransform>(), Margin, Margin, Width, FieldHeight);

        Image image = go.GetComponent<Image>();
        image.color = style.buttonColor;

        Text text = UiKit.NewText(go.transform, "Text", "", 30, style.buttonTextColor, style.font);
        Text placeholder = UiKit.NewText(go.transform, "Placeholder", "upgrade name...", 30,
                                         style.disabledTextColor, style.font);
        foreach (Text t in new[] { text, placeholder })
        {
            // InputField scrolls a single line itself; it needs the text clipped to its rect.
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.alignment = TextAnchor.MiddleLeft;
            t.supportRichText = false;
            RectTransform rt = t.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(16f, 0f);
            rt.offsetMax = new Vector2(-16f, 0f);
        }

        InputField input = go.GetComponent<InputField>();
        input.targetGraphic = image;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.lineType = InputField.LineType.SingleLine;
        input.caretColor = style.buttonHighlightColor;
        input.customCaretColor = true;
        input.selectionColor = new Color(style.buttonHighlightColor.r, style.buttonHighlightColor.g,
                                         style.buttonHighlightColor.b, 0.4f);
        // No keyboard navigation away from the field: arrows move the caret.
        Navigation nav = input.navigation;
        nav.mode = Navigation.Mode.None;
        input.navigation = nav;
        return input;
    }

    Text Corner(Transform parent, string name, string content, int size, Color color, float y)
    {
        Text t = UiKit.NewText(parent, name, content, size, color, style.font);
        t.alignment = TextAnchor.LowerLeft;
        Place(t.rectTransform, Margin, y, Width, size * 1.3f);
        return t;
    }

    // Bottom-left anchored rect, in 1920x1080 units.
    static void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }
}
