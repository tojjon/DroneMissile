using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The run's screens, polled from RunManager (docs/design/game-structure.md, waves.md):
/// - always: WAVE n / total, and NEXT SHOT +k while a yellow-ring bonus is pending;
/// - boss wave: the boss's health bar across the top of the screen (the exception to #13);
/// - between waves: "Wave N cleared", the wave's stats, three card slots (no upgrades exist yet,
///   so they say so) and Continue;
/// - end of run: RUN OVER / VICTORY with Stats and Upgrades tabs and a Main menu button.
///
/// Built in code with UiKit, like MainMenu. DroneHUD (stun border, crosshair, death message) stays
/// separate and underneath - this canvas sorts above it.
/// </summary>
public class RunUI : MonoBehaviour
{
    public UiStyle style = new UiStyle();

    [Header("Colours")]
    public Color bonusColor = new Color(1f, 0.85f, 0.1f, 1f);
    public Color bossBarColor = new Color(0.85f, 0.1f, 0.1f, 1f);
    public Color bossBarBackground = new Color(0f, 0f, 0f, 0.6f);
    public Color cardColor = new Color(1f, 1f, 1f, 0.08f);

    private RunManager run;
    private Shoting gun;

    private Text waveText;
    private Text bonusText;

    private GameObject bossBar;
    private RectTransform bossFill;

    private GameObject intermission;
    private Text intermissionTitle;
    private Text intermissionStats;
    private GameObject continueButton;

    private GameObject endScreen;
    private Text endTitle;
    private Text endStats;
    private Text endUpgrades;
    private GameObject menuButton;

    private RunManager.State shown = RunManager.State.Playing;
    private GameObject currentFirst;

    void Start()
    {
        run = RunManager.Instance;
        if (run == null)
        {
            Debug.LogWarning("RunUI: no RunManager in the scene. Run UI disabled.");
            enabled = false;
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) gun = player.GetComponent<Shoting>();

        style.font = UiKit.BuiltinFont();
        UiKit.EnsureEventSystem();
        Build();
        Show(RunManager.State.Playing);
    }

    void Update()
    {
        waveText.text = "WAVE " + run.CurrentWave + " / " + run.WaveCount;

        int bonus = gun != null ? gun.PendingBonus : 0;
        bonusText.gameObject.SetActive(bonus > 0);
        if (bonus > 0) bonusText.text = "NEXT SHOT +" + bonus;

        EnemyTurret boss = run.Boss;
        bool bossAlive = boss != null && run.CurrentState == RunManager.State.Playing;
        bossBar.SetActive(bossAlive);
        if (bossAlive) bossFill.anchorMax = new Vector2(boss.HealthFraction, 1f);

        if (run.CurrentState != shown) Show(run.CurrentState);
        UiKit.KeepSelection(currentFirst);
    }

    void Show(RunManager.State state)
    {
        shown = state;
        intermission.SetActive(state == RunManager.State.Intermission);
        endScreen.SetActive(state == RunManager.State.Ended);
        currentFirst = null;

        if (state == RunManager.State.Intermission)
        {
            intermissionTitle.text = "WAVE " + run.CurrentWave + " CLEARED";
            intermissionStats.text =
                "Damage this wave:  " + GameSession.DamageThisWave + "\n" +
                "Damage total:  " + GameSession.DamageTotal + "\n" +
                "Turrets destroyed:  " + GameSession.TurretsDestroyed;
            currentFirst = continueButton;
        }
        else if (state == RunManager.State.Ended)
        {
            endTitle.text = run.Victory ? "VICTORY" : "RUN OVER";
            endStats.text =
                "Difficulty:  " + GameSession.CurrentDifficulty + "\n" +
                "Wave reached:  " + run.CurrentWave + " / " + run.WaveCount + "\n" +
                "Best wave:  " + GameSession.BestWave + (run.NewRecord ? "   NEW RECORD!" : "") + "\n" +
                "Damage last wave:  " + GameSession.DamageThisWave + "\n" +
                "Damage total:  " + GameSession.DamageTotal + "\n" +
                "Turrets destroyed:  " + GameSession.TurretsDestroyed;
            ShowTab(true);
            currentFirst = menuButton;
        }

        if (currentFirst != null)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            UiKit.Select(currentFirst);
        }
    }

    void ShowTab(bool stats)
    {
        endStats.gameObject.SetActive(stats);
        endUpgrades.gameObject.SetActive(!stats);
    }

    // ---- construction -----------------------------------------------------------------------

    void Build()
    {
        GameObject canvasGo = UiKit.NewCanvas(transform, 200);   // above DroneHUD (100)
        Transform root = canvasGo.transform;

        // Wave counter and ring bonus, top-left.
        waveText = Corner(root, "Wave", new Vector2(40f, -30f), style.titleColor);
        bonusText = Corner(root, "Bonus", new Vector2(40f, -80f), bonusColor);

        BuildBossBar(root);
        BuildIntermission(root);
        BuildEndScreen(root);
    }

    Text Corner(Transform parent, string name, Vector2 pos, Color color)
    {
        Text t = UiKit.NewText(parent, name, "", 40, color, style.font);
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.UpperLeft;
        RectTransform rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(600f, 50f);
        t.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.7f);
        return t;
    }

    // Anchor-driven fill, the same technique as TurretHealthBar - no sprite, no Image.Type.Filled.
    void BuildBossBar(Transform parent)
    {
        bossBar = new GameObject("BossBar", typeof(RectTransform));
        bossBar.transform.SetParent(parent, false);
        RectTransform rt = bossBar.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -40f);
        rt.sizeDelta = new Vector2(900f, 34f);

        UiKit.NewFill<Image>(bossBar.transform, "Background").color = bossBarBackground;

        Image fill = UiKit.NewFill<Image>(bossBar.transform, "Fill");
        fill.color = bossBarColor;
        bossFill = fill.rectTransform;
        bossFill.offsetMin = new Vector2(3f, 3f);
        bossFill.offsetMax = new Vector2(-3f, -3f);

        Text label = UiKit.NewText(bossBar.transform, "Label", "BOSS", 26, Color.white, style.font);
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        RectTransform lrt = label.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        bossBar.SetActive(false);
    }

    void BuildIntermission(Transform parent)
    {
        intermission = CentrePanel(parent, "Intermission", new Vector2(1100f, 720f));
        Transform col = PanelColumn(intermission.transform, 1000f);

        intermissionTitle = Heading(col, "");
        intermissionStats = Body(col, 150f);

        // Card slots. The card system plugs in here once upgrades exist (game-structure.md, q. 2).
        GameObject row = new GameObject("Cards", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(col, false);
        row.GetComponent<RectTransform>().sizeDelta = new Vector2(1000f, 240f);
        HorizontalLayoutGroup h = row.GetComponent<HorizontalLayoutGroup>();
        h.spacing = 30f;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = h.childControlHeight = false;
        h.childForceExpandWidth = h.childForceExpandHeight = false;
        for (int i = 0; i < 3; i++)
        {
            Image card = UiKit.NewRect<Image>(row.transform, "Card");
            card.color = cardColor;
            card.raycastTarget = false;
            card.rectTransform.sizeDelta = new Vector2(280f, 220f);
            Text t = UiKit.NewText(card.transform, "Text", "No upgrades yet", 26, style.disabledTextColor, style.font);
            t.alignment = TextAnchor.MiddleCenter;
            RectTransform trt = t.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
        }

        continueButton = UiKit.NewButton(col, "Continue", () => run.ContinueToNextWave(), style).gameObject;
        intermission.SetActive(false);
    }

    void BuildEndScreen(Transform parent)
    {
        endScreen = CentrePanel(parent, "EndScreen", new Vector2(1000f, 760f));
        Transform col = PanelColumn(endScreen.transform, 900f);

        endTitle = Heading(col, "");

        GameObject tabs = new GameObject("Tabs", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        tabs.transform.SetParent(col, false);
        tabs.GetComponent<RectTransform>().sizeDelta = new Vector2(900f, style.buttonSize.y);
        HorizontalLayoutGroup h = tabs.GetComponent<HorizontalLayoutGroup>();
        h.spacing = 20f;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = true;
        UiKit.NewButton(tabs.transform, "Stats", () => ShowTab(true), style);
        UiKit.NewButton(tabs.transform, "Upgrades", () => ShowTab(false), style);

        endStats = Body(col, 300f);
        // Placeholder until the card system exists - this is where the run's picks will be listed.
        endUpgrades = Body(col, 300f);
        endUpgrades.text = "No upgrades picked.";
        endUpgrades.color = style.disabledTextColor;

        menuButton = UiKit.NewButton(col, "Main menu", () => SceneManager.LoadScene(GameSession.MainMenuScene), style).gameObject;
        endScreen.SetActive(false);
    }

    GameObject CentrePanel(Transform parent, string name, Vector2 size)
    {
        Image bg = UiKit.NewRect<Image>(parent, name);
        bg.color = new Color(0f, 0f, 0f, 0.8f);
        RectTransform rt = bg.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        return bg.gameObject;
    }

    Transform PanelColumn(Transform panel, float width)
    {
        return UiKit.NewColumn(panel, "Column", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                               new Vector2(0f, -40f), width, 24f, TextAnchor.UpperCenter).transform;
    }

    Text Heading(Transform parent, string content)
    {
        Text t = UiKit.NewText(parent, "Title", content, style.titleFontSize, style.titleColor, style.font);
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.rectTransform.sizeDelta = new Vector2(0f, style.titleFontSize * 1.3f);
        return t;
    }

    Text Body(Transform parent, float height)
    {
        Text t = UiKit.NewText(parent, "Body", "", 34, style.titleColor, style.font);
        t.alignment = TextAnchor.UpperCenter;
        t.lineSpacing = 1.2f;
        t.rectTransform.sizeDelta = new Vector2(0f, height);
        return t;
    }
}
