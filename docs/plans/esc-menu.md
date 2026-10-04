# ESC menu (pauza)

**Stav:** plánováno. Dnes Esc ve hře a v sandboxu **rovnou vrací do main menu** — dočasná
zkratka (`ReturnToMenu`), která se s tímhle smaže.

## Co to má být `[ZADÁNÍ]`

**Esc** hru zastaví a ukáže menu:

| Položka | Co dělá |
|---|---|
| **Resume** | pokračuje ve hře (Esc znovu taky) |
| **Upgrades** | ukáže upgrady, které dron právě má ([upgrades.md](upgrades.md)) |
| **Main menu** | návrat do hlavního menu |
| **Quit** | ukončí hru |

## Návrh implementace

- Stejné UI jako ostatní menu (`UiKit`), pauza přes `Time.timeScale = 0` jako obrazovka mezi vlnami.
- Funguje v aréně i v sandboxu. Na obrazovce mezi vlnami a na konci runu Esc nic nedělá (už je
  pauza).
- Dron s Rigidbody musí pauzu přežít beze změny; `Shoting` už na pauze nestřílí.
- **Po dokončení smazat** `ReturnToMenu.cs` a objekt `ReturnToMenu` ze scén (a z builderů
  `MainMenuBuilder` / `ArenaBuilder`).

## Otevřené otázky `[OTEVŘENÉ]`

1. **Main menu z rozehraného runu** — run se zahodí, nebo uloží pro Continue
   ([save-continue.md](save-continue.md))?
2. **Má mít ESC menu i nastavení** (hlasitost, citlivost, výběr vysílačky)? Zadání ho nemá.
3. **Otevření z vysílačky** — přepínač na vysílačce jako Esc?

## Souvisí

[design/game-structure.md](../design/game-structure.md), [features/controls.md](../features/controls.md).
