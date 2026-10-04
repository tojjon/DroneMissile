# Main menu

**Stav:** hotovo · otestováno hraním: ano (04.10.2026) · `[OVĚŘENO 04.10.2026]`

## Co to je

Hra startuje do scény `MainMenu`. V pozadí kamera pomalu krouží kolem dronu nad terénem, vlevo je
sloupec tlačítek.

| Tlačítko | Co dělá |
|---|---|
| **Continue** | **zašedlé** — ukládání zatím neexistuje |
| **New game** | výběr **Easy / Normal / Hardcore** → spustí run v [aréně](arena.md) |
| **Sandbox** | načte [sandbox](sandbox.md) |
| **Upgrades** | katalog všech upgradů: karta, rarita, popis ([upgrades.md](upgrades.md)) |
| **Quit** | ukončí hru (navíc proti zadání) |

Ovládá se **myší nebo šipkami + Enter**. Vysílačkou zatím ne. Z gameplaye se zpět dostaneš **Esc**
(dočasně, než vznikne ESC menu).

## Kde v projektu

- `Assets/Scenes/MainMenu.unity` — kopie `SampleScene` bez hratelnosti, vyrábí ji
  `Tools > DroneMissile > Build Main Menu Scene`. **Ruční úpravy scény přepíše další spuštění.**
- `MainMenu.cs` (UI), `MenuCameraOrbit.cs` (kamera), `UiKit.cs` (sdílená stavebnice UI),
  `GameSession.cs` (obtížnost, názvy scén).

## Zbývá

ESC pauza, Continue + ukládání, katalog upgradů, ovládání vysílačkou.

## Souvisí

[design/game-structure.md](../design/game-structure.md),
[plans/archive/main-menu.md](../plans/archive/main-menu.md), rozhodnutí [#21](../decisions.md).
