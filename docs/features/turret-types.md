# Čtyři typy turretů

**Stav:** hotovo · otestováno hraním: ano (sandbox, 04.10.2026) · `[OVĚŘENO 04.10.2026]`

## Co to je

Čtyři barevné varianty [turretu](turret.md). **Barva říká, co střílí.** Všechny omračují — dron
nemá HP ([rozhodnutí #10](../decisions.md)).

| Typ | Projektil | Co dělá | Vzhled stunu |
|---|---|---|---|
| **Šedý** | kámen (`enemy_rock`) | rychlá palba, 1080 m/s, **většinou proletí skrz** (~6 % zásahů) — stejně jako původní nepřátelská raketa | Rock (žluté úlomky) |
| **Modrý** | elektrický náboj (`enemy_electric`) | kde dopadne, odtud **přeskočí blesk na dron**, pokud je do `zapRange`; přímý zásah taky | Electric (modré výboje) |
| **Červený** | granát (`enemy_explosive`) | **vybuchne o cokoli** a omráčí vše v `blastRadius` — nemusí trefit přímo | Rock |
| **Zelený** | naváděná raketa (`enemy_homing`) | **zatáčí za dronem** omezenou rychlostí a po `lifeTime` vyhoří; dá se jí uletět nebo vykličkovat | Rock |

## Ladění (prefaby projektilů v `Assets/3D models/`)

| | `speed` | `lifeTime` | `fireRate` turretu | stun | navíc |
|---|---|---|---|---|---|
| Šedý | 1080 | 5 s | 0,2 s | 1 s | `visualPrefab` = slot na model kamene |
| Modrý | 70 | 6 s | 1,5 s | 1 s | `zapRange` 8 m |
| Červený | 50 | 8 s | 2,5 s | 1 s | `blastRadius` 10 m |
| Zelený | 40 | 7 s | 3 s | 1 s | `turnRate` 70 °/s |

Pole `stunKind` na projektilu vybírá vzhled stunu. Šedý používá diskrétní trigger (prolétává),
ostatní tři **sweep** (spolehlivé zásahy).

## Kde v projektu

- Prefaby turretů: `Assets/Prefabs/Turrets/Turret_{Grey,Blue,Red,Green}.prefab`
- Skripty: `EnemyProjectile.cs` (základ = šedý), `ElectricProjectile.cs`, `ExplosiveProjectile.cs`,
  `HomingProjectile.cs`, `ElectricZap.cs`
- Generuje `Tools > DroneMissile > Build Turret Types` — vytváří jen chybějící assety, ladění
  v Inspectoru přežije.

## Zbývá

- **Model kamene** pro šedý turret — Viktor dodá, patří do `visualPrefab` na `enemy_rock`.
- Žlutá barva „kamenného" stunu je dočasná.

## Souvisí

[stun.md](stun.md), [sandbox.md](sandbox.md), [waves.md](waves.md),
[design/enemies.md](../design/enemies.md), [plans/archive/turret-types.md](../plans/archive/turret-types.md),
rozhodnutí [#22](../decisions.md).
