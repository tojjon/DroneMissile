using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The main menu: Continue / New game / Sandbox / Upgrades / Quit, a difficulty picker behind
/// New game, and an Upgrades placeholder. Spec is docs/design/game-structure.md.
///
/// Built in code like DroneHUD, for the same reasons (scenes are not hand-edited as YAML, and the
/// whole feature stays one reviewable file). Unlike the HUD it takes input - docs/decisions.md #21.
/// The building blocks are shared with RunUI through UiKit.
/// </summary>
public class MainMenu : MonoBehaviour
{
    public string title = "DRONE MISSILE";

    [Tooltip("Distance of the button column from the left screen edge, in 1920x1080 units.")]
    public float leftMargin = 140f;

    public UiStyle style = new UiStyle();

    private GameObject mainPanel;
    private GameObject difficultyPanel;
    private GameObject upgradesPanel;
    private GameObject catalog;

    // What gets selected when a panel opens, so arrows + Enter work without touching the mouse.
    private GameObject mainFirst;
    private GameObject difficultyFirst;
    private GameObject upgradesFirst;
    private GameObject currentFirst;

    void Start()
    {
        // Whatever gameplay or a pause screen left behind, the menu needs real time and a cursor.
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Back in the menu = the last run is over; its upgrades must not leak into the sandbox or
        // the next run.
        GameSession.ResetRun();

        style.font = UiKit.BuiltinFont();
        UiKit.EnsureEventSystem();
        Build();
        Show(mainPanel, mainFirst);
    }

    void Update()
    {
        UiKit.KeepSelection(currentFirst);
    }

    // ---- actions ----------------------------------------------------------------------------

    void OnContinue()
    {
        // Unreachable while GameSession.HasSave is false - the button is not interactable.
    }

    void OnStartGame(GameSession.Difficulty difficulty)
    {
        GameSession.CurrentDifficulty = difficulty;
        SceneManager.LoadScene(GameSession.GameScene);
    }

    void OnSandbox()
    {
        SceneManager.LoadScene(GameSession.SandboxScene);
    }

    void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Show(GameObject panel, GameObject first)
    {
        mainPanel.SetActive(panel == mainPanel);
        difficultyPanel.SetActive(panel == difficultyPanel);
        upgradesPanel.SetActive(panel == upgradesPanel);
        catalog.SetActive(panel == upgradesPanel);

        currentFirst = first;
        UiKit.Select(first);
    }

    // ---- construction -----------------------------------------------------------------------

    void Build()
    {
        GameObject canvasGo = UiKit.NewCanvas(transform, 0);

        // A dark band down the left third keeps the text readable over the moving backdrop.
        Image band = UiKit.NewRect<Image>(canvasGo.transform, "Band");
        RectTransform bandRt = band.rectTransform;
        bandRt.anchorMin = new Vector2(0f, 0f);
        bandRt.anchorMax = new Vector2(0f, 1f);
        bandRt.pivot = new Vector2(0f, 0.5f);
        bandRt.sizeDelta = new Vector2(leftMargin * 2f + style.buttonSize.x, 0f);
        bandRt.anchoredPosition = Vector2.zero;
        band.color = style.panelColor;
        band.raycastTarget = false;

        Text titleText = UiKit.NewText(canvasGo.transform, "Title", title, style.titleFontSize, style.titleColor, style.font);
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleLeft;
        RectTransform titleRt = titleText.rectTransform;
        titleRt.anchorMin = titleRt.anchorMax = new Vector2(0f, 1f);
        titleRt.pivot = new Vector2(0f, 1f);
        titleRt.anchoredPosition = new Vector2(leftMargin, -140f);
        titleRt.sizeDelta = new Vector2(1000f, style.titleFontSize * 1.4f);

        // Main
        mainPanel = NewColumn(canvasGo.transform, "MainPanel");
        Button cont = UiKit.NewButton(mainPanel.transform, "Continue", OnContinue, style);
        UiKit.SetInteractable(cont, GameSession.HasSave, style);
        Button newGame = UiKit.NewButton(mainPanel.transform, "New game", () => Show(difficultyPanel, difficultyFirst), style);
        UiKit.NewButton(mainPanel.transform, "Sandbox", OnSandbox, style);
        UiKit.NewButton(mainPanel.transform, "Upgrades", () => Show(upgradesPanel, upgradesFirst), style);
        UiKit.NewButton(mainPanel.transform, "Quit", OnQuit, style);
        mainFirst = (cont.interactable ? cont : newGame).gameObject;

        // Difficulty - what each one means is docs/decisions.md #19, #20 and #25.
        difficultyPanel = NewColumn(canvasGo.transform, "DifficultyPanel");
        UiKit.NewLabel(difficultyPanel.transform, "Choose difficulty", style);
        Button easy = UiKit.NewButton(difficultyPanel.transform, "Easy", () => OnStartGame(GameSession.Difficulty.Easy), style);
        UiKit.NewButton(difficultyPanel.transform, "Normal", () => OnStartGame(GameSession.Difficulty.Normal), style);
        UiKit.NewButton(difficultyPanel.transform, "Hardcore", () => OnStartGame(GameSession.Difficulty.Hardcore), style);
        UiKit.NewButton(difficultyPanel.transform, "Back", () => Show(mainPanel, mainFirst), style);
        difficultyFirst = easy.gameObject;

        // Upgrades - a catalog once upgrades exist; there are none yet (game-structure.md, question 2).
        upgradesPanel = NewColumn(canvasGo.transform, "UpgradesPanel");
        UiKit.NewLabel(upgradesPanel.transform, "Upgrades", style);
        if (UpgradeCatalog.All.Count == 0)
        {
            UiKit.NewLabel(upgradesPanel.transform, "No upgrades yet.", style).color = style.disabledTextColor;
        }
        upgradesFirst = UiKit.NewButton(upgradesPanel.transform, "Back", () => Show(mainPanel, mainFirst), style).gameObject;
        catalog = BuildCatalog(canvasGo.transform);
    }

    // Every upgrade: card art, name, what it does. Right of the button column, on its own dark panel.
    GameObject BuildCatalog(Transform parent)
    {
        float left = leftMargin * 2f + style.buttonSize.x + 40f;

        Image bg = UiKit.NewRect<Image>(parent, "Catalog");
        bg.color = style.panelColor;
        bg.raycastTarget = false;
        RectTransform rt = bg.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(left, 80f);
        rt.offsetMax = new Vector2(-60f, -80f);

        GridLayoutGroup grid = bg.gameObject.AddComponent<GridLayoutGroup>();
        grid.padding = new RectOffset(30, 30, 30, 30);
        grid.cellSize = new Vector2(320f, 420f);
        grid.spacing = new Vector2(30f, 30f);
        grid.childAlignment = TextAnchor.UpperLeft;

        foreach (UpgradeDefinition def in UpgradeCatalog.All)
        {
            GameObject cell = new GameObject(def.displayName, typeof(RectTransform));
            cell.transform.SetParent(bg.transform, false);

            RectTransform art = UiKit.NewCardArt(cell.transform, def.card, 2f).rectTransform;
            art.anchorMin = art.anchorMax = art.pivot = new Vector2(0.5f, 1f);
            art.anchoredPosition = Vector2.zero;

            Text name = UiKit.NewText(cell.transform, "Name", def.displayName + "  (" + def.rarity + ")", 28,
                                      Rarities.Colour(def.rarity), style.font);
            name.fontStyle = FontStyle.Bold;
            name.alignment = TextAnchor.UpperCenter;
            Place(name.rectTransform, -UiKit.CardH * 2f - 12f, 40f);

            Text desc = UiKit.NewText(cell.transform, "Description", def.description, 22, style.titleColor, style.font);
            desc.alignment = TextAnchor.UpperCenter;
            desc.horizontalOverflow = HorizontalWrapMode.Wrap;
            Place(desc.rectTransform, -UiKit.CardH * 2f - 56f, 150f);
        }
        return bg.gameObject;
    }

    // Full-width strip inside a catalog cell, `top` below the cell's top edge.
    static void Place(RectTransform rt, float top, float height)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, height);
        rt.anchoredPosition = new Vector2(0f, top);
    }

    // A vertical stack of buttons anchored left, below the title.
    GameObject NewColumn(Transform parent, string name)
    {
        return UiKit.NewColumn(parent, name, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                               new Vector2(leftMargin, -60f), style.buttonSize.x, style.buttonSpacing,
                               TextAnchor.UpperLeft);
    }
}
