# Plán: Main menu a Sandbox

**Plán ze session 04.10.2026.** Stav: **implementováno** (commit `0db0288`), otestováno hraním.
Hotová podoba je v [features/main-menu.md](../features/main-menu.md) a
[features/sandbox.md](../features/sandbox.md). Rozhodnutí je [#21](../decisions.md).

## Kontext

Rozhodnutí [#18](../decisions.md) mění hru na run po vlnách, který začíná v **hlavním menu**
([design/game-structure.md](../design/game-structure.md)). Do té doby měl build jedinou scénu
(`SampleScene`) a hra startovala rovnou do letu. Tohle byl první kus předělání: samotné menu a scéna
`Sandbox`, aby tlačítko Sandbox mělo kam vést.

## Dohodnuto s Viktorem

- **New game** → výběr Easy / Normal → volba se zapamatuje → načte se dnešní `SampleScene` jako
  zástupce, dokud neexistuje aréna. **Continue** je zašedlé — ukládání neexistuje.
- **Sandbox** → nová scéna `Sandbox`, **kopie `SampleScene`** (terén, plošina, turret). Pole na
  psaní jména upgradu do rozsahu nepatří — upgrady zatím nejsou.
- **Vzhled:** 3D pozadí (kamera pomalu krouží nad terénem kolem dronu) + sloupec tlačítek.
- **Ovládání:** myš + klávesnice (šipky / Enter). Vysílačkou ne.

## Přístup

Stejné vzory jako zbytek projektu: **UI se staví v C# za běhu** (jako `DroneHUD`) a **scény se
vyrábí jen editorovými buildery** (jako `HudBuilder`), nikdy ruční úpravou YAML.

### Nové runtime skripty

1. **`GameSession`** — statická třída, přežije načtení scény. `Difficulty` (Easy / Normal), zvolená
   obtížnost, `HasSave` (zatím vždy false) a názvy scén na jednom místě.
2. **`MainMenu`** — postaví screen-space canvas stejně jako `DroneHUD`, ale **bere input**: přidá
   `GraphicRaycaster` a `EventSystem` s `InputSystemUIInputModule` (projekt jede jen na novém Input
   Systemu). Tři panely: hlavní, výběr obtížnosti, placeholder upgradů. Při ztrátě výběru vrátí
   fokus na první tlačítko, aby šipky vždy fungovaly.
3. **`MenuCameraOrbit`** — kamera krouží kolem pivotu.
4. **`ReturnToMenu`** — **dočasně**: Esc ve hře vrací do menu, dokud nevznikne ESC menu.

### Editorový builder `MainMenuBuilder`

- **Build Main Menu Scene** — zkopíruje `SampleScene` → `MainMenu.unity` a odstraní hratelnost:
  HUD, ovládání a fyziku dronu, tag `Player` (turret pak nemá cíl a nestřílí); kamera se odpojí od
  dronu a dostane `MenuCameraOrbit`.
- **Build Sandbox Scene** — zkopíruje `SampleScene` → `Sandbox.unity` a přidá `ReturnToMenu`.
- **Configure Build Scenes** — `MainMenu` jako index 0, aby build startoval do menu.

Kopie jsou snapshoty — pozdější úpravy `SampleScene` se do nich nepropíšou, dokud se builder
nespustí znovu.

## Ověření

1. Headless kompilace (editor zavřený).
2. Viktor v editoru: kamera krouží, Continue zašedlé, šipky + Enter fungují, New game → Easy načte
   hru, Sandbox načte sandbox, Esc vrací do menu, Quit ukončí Play mode, turret v pozadí nestřílí.

## Mimo rozsah

ESC pauza, ukládání / Continue, systém upgradů a katalog, konzole upgradů v sandboxu, aréna, vlny.

## Odchylky při implementaci

- **Quit** přibyl navíc oproti zadání.
- UI stavebnice se později přesunula do sdíleného `UiKit` (plán [arena-run.md](arena-run.md)).
- Po vzniku arény vede New game do scény `Arena`, ne do `SampleScene`.
