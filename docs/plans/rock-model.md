# Model kamene a „kamenný" stun

**Stav:** rozpracováno — model dodán (`Assets/Prefabs/Ammo/Rock.fbx`), zapojuje se.

## Co to má být `[ZADÁNÍ]`

Šedý turret střílí **kameny**. Viktor dodá model kamene. Stun od šedého má mít **kamenný vibe** —
dnes je to žlutý placeholder.

## Co už je připravené

- **Slot na model:** pole **`Visual Prefab`** na prefabu `Assets/3D models/enemy_rock.prefab`
  (komponenta `EnemyProjectile`). Když je vyplněné, projektil schová svou krychli a ukáže model.
  Je to jen vizuál — collider zůstává prefabu.
- Boss střílí **velké kameny** (`enemy_boss_rock.prefab`) — má stejný slot, model se tam dá použít
  taky.
- **Vzhled stunu** „Rock" je druhá instance `DroneStunArcs` na dronu: žluté padající úlomky. Barva,
  velikost, gravitace a množství se ladí v Inspectoru na dronu (instance s `Kind = Rock`).

## Plán implementace

1. `EnemyProjectile` vyruší neuniformní scale kořene projektilu na vizuálu — kořen `enemy_rock` je
   tyč 0.1 × 0.1 × 0.99, takže dítě by bylo jehla (stejná past jako rozhodnutí #4). Nové pole
   `visualScale` (1 = prefab tak, jak je; boss 3).
2. `Assets/Editor/RockModelBuilder.cs` (`Tools > DroneMissile > Apply Rock Model`): obalí
   `Rock.fbx` do `Assets/Prefabs/Ammo/RockVisual.prefab` — vycentrovaný na bounds meshe, největší
   rozměr 1 m, bez colliderů / kamer / světel z exportu — a zapíše ho do `Visual Prefab` na
   `enemy_rock` (`visualScale` 1) a `enemy_boss_rock` (3, sedí na jeho `sweepRadius` 1.5 m).
   Vyrábí jen chybějící věci, ladění v Inspectoru přežije opakované spuštění.
3. Kámen se za letu točí: `visualSpin` (°/s, výchozí 360) — současně kolem X, Y i Z, každá osa náhodně 50–100 % a náhodný směr, z náhodné počáteční
   orientací, pro každou ránu jinak. Model visí pod mezilehlým `VisualHolder`, který ruší scale
   kořene — rotace přímo pod neuniformním scale by kámen zkosila.
4. Textury nejsou — kámen má materiál z Blenderu, jak ho Unity naimportovalo.

## Odchylky při implementaci

- Model leží v `Assets/Prefabs/Ammo/`, ne v `Assets/3D models/`, jak předpokládal původní postup.
- Šedý kámen má `sweepRadius` 0 — zásah se počítá tenkým paprskem středem, vizuál je 1 m. Kámen,
  který dron vizuálně „lízne", ho nezasáhne. Zatím ponecháno.

## Otevřené otázky `[OTEVŘENÉ]`

1. **Jak má vypadat kamenný stun?** Prach, úlomky, otřes kamery? Jakou barvu místo žluté?
2. ~~**Rotace kamene za letu** — má se točit?~~ Ano, po všech osách (Viktor, 05.10.2026).

## Souvisí

[features/turret-types.md](../features/turret-types.md), [features/stun.md](../features/stun.md),
[design/enemies.md](../design/enemies.md).
