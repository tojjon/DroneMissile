# Let dronu

**Stav:** hotovo · otestováno hraním: ano · `[OVĚŘENO 04.10.2026]`

## Co to je

Dron létá jako reálný FPV dron v **acro módu**: bez stropu rychlosti, bez stabilizace, bez
auto-levelu. Je to základ celé hry — když se střílení a let dostanou do sporu, ustupuje střílení
([concept.md](../concept.md)).

## Jak se to chová

- **Tah působí jen podél lokálního „nahoru" dronu.** Dopředu se letí naklopením a přidáním plynu —
  žádná samostatná síla dopředu neexistuje ([rozhodnutí #2](../decisions.md)).
- **Rotace nemá setrvačnost:** pustíš stick a otáčení okamžitě přestane (`MoveRotation`).
- Dron zůstane naklopený, dokud ho pilot sám nesrovná.
- Dotek čehokoli pevného je pád — viz [death-and-difficulty.md](death-and-difficulty.md).

## Ladění

| Parametr | Hodnota ve scéně | Kde |
|---|---|---|
| `throttleForce` | **0,4** | `DroneControls` |
| `pitchSpeed` / `rollSpeed` / `yawSpeed` | 100 / 100 / 100 | `DroneControls` |
| `deadzone` | 0,02 | `DroneControls` |
| hmotnost | 0,0075 kg | Rigidbody dronu |
| `quadraticDrag` | 0,015 /m | `DroneControls` — odpor ∝ rychlost²; `linearDamping` Rigidbody kód nuluje ([#30](../decisions.md)) |
| `gravityMultiplier` | 1,5 × | `DroneControls` — dron padá rychleji, tah se tím neškáluje |
| `angularDamping` | 0,5 | Rigidbody dronu |
| detekce kolizí | ContinuousDynamic (nastavuje kód) | `DroneControls.Start()` |

Pozor: [design/flight-model.md](../design/flight-model.md) uvádí `throttleForce` 15 — ve scéně je
0,4. Platí scéna.

## Kde v projektu

`Assets/scripts/DroneControls.cs` (`FixedUpdate`), Rigidbody na objektu `Drone`.

## Souvisí

[controls.md](controls.md), [design/flight-model.md](../design/flight-model.md),
rozhodnutí [#1](../decisions.md), [#2](../decisions.md), [#11](../decisions.md).
