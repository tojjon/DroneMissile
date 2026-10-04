# Model kamene a „kamenný" stun

**Stav:** rozpracováno — kód je připravený, **čeká se na Viktorův model.**

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

## Jak model vložit

1. Model (FBX / blend) do `Assets/3D models/`.
2. Udělat z něj prefab (přetáhnout do Project okna).
3. Prefab přetáhnout do `Visual Prefab` na `enemy_rock` (a případně `enemy_boss_rock`).
4. Velikost modelu sladit s colliderem projektilu (zhruba 1 m).

## Otevřené otázky `[OTEVŘENÉ]`

1. **Jak má vypadat kamenný stun?** Prach, úlomky, otřes kamery? Jakou barvu místo žluté?
2. **Rotace kamene za letu** — má se točit?

## Souvisí

[features/turret-types.md](../features/turret-types.md), [features/stun.md](../features/stun.md),
[design/enemies.md](../design/enemies.md).
