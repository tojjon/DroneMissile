# Editorové nástroje (Tools > DroneMissile)

**Stav:** hotovo · `[OVĚŘENO 04.10.2026]`

## Co to je

Scény a prefaby se v tomhle projektu **nikdy needitují ručně jako YAML** (rozbily by se GUID
reference). Všechno, co by se jinak skládalo ručně ve scéně, vyrábí položky menu
**Tools > DroneMissile** v editoru. Jde je spustit i headless (`-executeMethod`).

| Položka | Co dělá | Přepíše ruční úpravy? |
|---|---|---|
| **Build HUD** | přidá objekt `HUD` do otevřené scény | ne |
| **Build Turret Body** | tvar turretu (hlaveň, hlava, collidery) | jen dotčené hodnoty |
| **Build Turret Health Bars** | pruh HP na všechny turrety ve scéně | ne |
| **Build Drone Stun Arcs** | elektrické výboje na dron | ne |
| **Build Turret Types** | materiály, projektily, 4 prefaby turretů, rozmístění v sandboxu, rock stun na dron | **ne** — vytváří jen chybějící assety |
| **Build Arena** | scéna `Arena`, prefaby bosse, build list | **scénu ano** (zeptá se), prefaby bosse ne |
| **Build All Menu Scenes** | `MainMenu` + `Sandbox` + Esc zkratka v `SampleScene` + build list | **ano** (zeptá se) |
| **Build Main Menu Scene** / **Build Sandbox Scene** | jednotlivě | **ano** (zeptá se) |
| **Add ReturnToMenu (temporary Esc)** | Esc → menu do otevřené scény | ne |
| **Configure Build Scenes** | build list: MainMenu, Arena, SampleScene, Sandbox | — |

## Důležité

- `MainMenu`, `Sandbox` a `Arena` jsou **kopie `SampleScene`**. Změny v `SampleScene` se do nich
  nepropíšou, dokud se příslušný builder nespustí znovu.
- **Pořadí po čistém klonu:** Build Turret Types → Build All Menu Scenes → Build Arena.
- Headless (editor musí být zavřený):
  `Unity -batchmode -quit -projectPath . -executeMethod ArenaBuilder.BuildAll`
  (na Linuxu přes Flatpak, viz [CLAUDE.md](../../CLAUDE.md)).

## Kde v projektu

`Assets/Editor/*.cs`.

## Souvisí

Rozhodnutí [#21](../decisions.md), [#22](../decisions.md), [#23](../decisions.md).
