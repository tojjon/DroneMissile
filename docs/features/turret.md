# Turret (základ všech nepřátel)

**Stav:** hotovo · otestováno hraním: ano · `[OVĚŘENO 04.10.2026]`

## Co to je

Pozemní věž, která míří na dron a střílí po něm. Všechny čtyři barevné typy i boss jsou tentýž
`EnemyTurret` — liší se projektilem a barvou ([turret-types.md](turret-types.md)).

## Jak se to chová

- **Vidí dron do 360 m**, bez line-of-sight — skrz terén i překážky.
- **Míří hlavní horizontálně i vertikálně**, otáčí se `turnSpeed` stupňů za sekundu; střílí podél
  hlavně, takže dokud se dotáčí, míjí.
- Má **HP** a po vyčerpání zmizí. Zbývající HP ukazuje pruh nad ním, viz
  [turret-health-bar.md](turret-health-bar.md).
- Po návratu dronu do dosahu střílí hned, bez náběhu ([rozhodnutí #15](../decisions.md)).

## Ladění (na prefabech / ve scéně)

| Parametr | Hodnota |
|---|---|
| `detectionRange` | 360 m |
| `turnSpeed` | 720 °/s |
| `maxHealth` | 100 (boss 600) |
| `fireRate` | podle typu, viz [turret-types.md](turret-types.md) |

## Kde v projektu

`Assets/scripts/EnemyTurret.cs`, prefaby `Assets/Prefabs/Turrets/`. Hierarchie
(`turret` → sourozenci `Turret_Base`, `Turret_Barrel`) není kosmetická, viz
[design/enemies.md](../design/enemies.md).

## Souvisí

Rozhodnutí [#4](../decisions.md), [#5](../decisions.md), [#9](../decisions.md),
[#14](../decisions.md), [#15](../decisions.md).
