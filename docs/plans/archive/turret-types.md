# Plán: Čtyři typy turretů

**Plán ze session 04.10.2026.** Stav: **implementováno** (commit `0db0288`), v sandboxu otestováno
hraním — všechny typy fungují podle záměru. Hotová podoba je v
[features/turret-types.md](../../features/turret-types.md) a [features/stun.md](../../features/stun.md).
Rozhodnutí je [#22](../../decisions.md).

## Kontext

Předělání ([#18](../../decisions.md), [design/enemies.md](../../design/enemies.md)) nahrazuje jediný
turret čtyřmi barevnými typy, které se liší tím, co střílí. Vlny a boss je budou spawnovat, takže
musí existovat jako **prefaby** — do té doby žil jediný turret jen ve scéně a žádný prefab neexistoval.

## Dohodnuto s Viktorem

| Typ | Projektil | Pravidlo zásahu |
|---|---|---|
| **Šedý** (stun) | kámen — model je pole `GameObject` v Inspectoru, Viktor dodá později | **beze změny proti dnešku**: trigger, 1080 m/s, prolétává (~6 % zásahů, [#14](../../decisions.md)). Stun s „kamenným" vzhledem, **zatím žlutý** |
| **Modrý** (electric) | elektrický náboj | spolehlivý sweep. Při dopadu **přeskočí výboj na dron, pokud je do `zapRange`**, a omráčí ho; přímý zásah omráčí taky. Na dronu hrají dnešní elektrické výboje |
| **Červený** (explozivní) | výbušný granát | spolehlivý sweep. Vybuchne o cokoli a **omráčí vše v `blastRadius`** |
| **Zelený** (homing) | naváděná raketa | spolehlivý sweep. Zásah = stun. Dá se jí uniknout: **omezené `turnRate` (°/s) + `lifeTime`** |

Testování: 4 prefaby turretů + po jednom v **Sandboxu**. `SampleScene` si nechá svůj turret.
Během práce Viktor doplnil: **turrety v sandboxu rozmístit daleko od sebe, ať nestřílí všechny
najednou.**

## Přístup

1. **Druhy stunu** — `DroneControls.Stun(sekundy, StunKind)` s `StunKind { Rock, Electric }`,
   uložený `LastStunKind`. Druh je pole na prefabu projektilu.
2. **Vzhled stunu na dronu** — `DroneStunArcs` se zobecní (pole `kind`, `noiseStrength`,
   `useTrails`, `gravity`, velikost částic) a dron nese **dvě instance**: Electric (dnešní výboje)
   a Rock (žluté padající úlomky). Červený rám HUD svítí u všech.
3. **Projektily** — `EnemyProjectile` se stane základem (sám o sobě je šedý). Nová pole `stunKind`,
   `useSweep`, `visualPrefab`; virtuální háčky `OnImpact`, `Steer`, `OnExpire`. Potomci
   `ElectricProjectile`, `ExplosiveProjectile`, `HomingProjectile`. Sweep je stejná technika jako
   u rakety hráče. Výboj modrého je částicový `ElectricZap` — **žádný `LineRenderer`**
   ([#17](../../decisions.md)).
4. **Assety — `TurretTypesBuilder`** — materiály, prefaby projektilů (`enemy_rock`,
   `enemy_electric`, `enemy_explosive`, `enemy_homing`), prefaby turretů
   (`Assets/Prefabs/Turrets/Turret_*`), rozmístění v sandboxu. Vytváří jen chybějící assety, takže
   ladění v Inspectoru přežije opakované spuštění.

### Výchozí hodnoty

| | speed | lifeTime | fireRate | stun | navíc |
|---|---|---|---|---|---|
| Šedý | 1080 | 5 s | 0,2 s | 1 s | — |
| Modrý | 70 | 6 s | 1,5 s | 1 s | `zapRange` 8 m |
| Červený | 50 | 8 s | 2,5 s | 1 s | `blastRadius` 10 m |
| Zelený | 40 | 7 s | 3 s | 1 s | `turnRate` 70 °/s |

## Ověření

Headless kompilace a build, kontrola zapojení prefabů; Viktor v sandboxu proletí ke každému typu.

## Mimo rozsah

Model kamene, boss a barevné kuličky, vlny, finální balanc.

## Odchylky při implementaci

- Turrety v sandboxu stojí **600 m od plošiny** do čtyř světových stran (dosah turretu je 360 m),
  původní turret 40 m od plošiny byl ze sandboxu odstraněn — střílel by hned po spawnu.
- `MainMenuBuilder` maže **všechny** instance `DroneStunArcs`, protože dron jich teď nese víc.
