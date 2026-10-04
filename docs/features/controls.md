# Ovládání (vysílačka a klávesnice)

**Stav:** hotovo · otestováno hraním: ano · `[OVĚŘENO 04.10.2026]`

## Co to je

Primární ovládání je **RC vysílačka** (RadioMaster Pocket, hlásí se jako HID joystick), klávesnice
je záloha na testování. Rozložení Mode 2.

## Mapování

| Akce | Klávesnice | Vysílačka (cesta) |
|---|---|---|
| Throttle | `LeftShift` / `LeftCtrl` | levý stick svisle (`z`) |
| Yaw | `Q` / `E` | levý stick vodorovně (`rx`) |
| Pitch | `W` / `S` | pravý stick svisle (`stick/y`) |
| Roll | `A` / `D` | pravý stick vodorovně (`stick/x`) |
| Výstřel | `Space` | `trigger` |

Pitch a roll jsou invertované (Viktorova preference). Throttle se přepočítává z −1..1 na 0..1.

V menu se ovládá myší nebo šipkami + Enter. **Esc** ve hře a v sandboxu zatím vrací rovnou do menu
(dočasně, `ReturnToMenu`).

## Známé věci

- Hledání vysílačky je **zkopírované** v `DroneControls.Start()` a `Shoting.Start()` — změna
  mapování se musí udělat v obou.
- Chybějící control vrací **0 bez chyby** — špatná cesta se projeví jen tím, že osa nic nedělá.
- `DroneControls.Start()` vypisuje všechna zařízení a jejich controly — je to záměrný nástroj na
  kalibraci nové vysílačky ([rozhodnutí #7](../decisions.md)).

## Kde v projektu

`Assets/scripts/DroneControls.cs`, `Assets/scripts/Shoting.cs`.

## Souvisí

[design/controls.md](../design/controls.md),
[reference/radiomaster-pocket.md](../reference/radiomaster-pocket.md),
rozhodnutí [#6](../decisions.md), [#7](../decisions.md).
