# Boss (vlny 10, 20, 30)

**Stav:** hotovo · otestováno hraním: **zatím ne** · `[OVĚŘENO 04.10.2026]`

## Co to je

**Velký šedý turret** (třikrát větší) na desáté vlně. Pruh HP má **nahoře na obrazovce**, ne nad
sebou.

## Jak se to chová

- **600 HP** = 60 zásahů raketou.
- Střílí **velké kameny**: 150 m/s, trefují podle své velikosti (sweep o poloměru 1,5 m), stun
  **2 s**.
- Při **50 % HP** vystřelí nahoru **salvu 6 barevných kuliček** do různých směrů, pak znovu
  **každých 25 s**, dokud žije. Kde kulička dopadne na podlahu, **vyroste turret její barvy**
  (barva náhodně z povolených: vlna 10 všechny čtyři, **vlna 20 bez šedé**, **vlna 30 jen zelená**
  — pole `bossBalls` v tabulce vln). Od stěny se kulička odrazí.
- **Vlna končí, až padne boss i všechny přivolané turrety** — včetně kuliček, které ještě letí.
- Boss je na všech třech vlnách tentýž (600 HP); liší se jen barvy kuliček.
- Vyčištění třicáté vlny = **VICTORY** ([end-screen.md](end-screen.md)).

## Ladění

| Parametr | Hodnota | Kde |
|---|---|---|
| `maxHealth` | 600 | `EnemyTurret` na `Turret_Boss` |
| `fireRate` | 1,2 s | `EnemyTurret` na `Turret_Boss` |
| `ballCount` | 6 | `BossTurret` |
| `volleyInterval` | 25 s | `BossTurret` |
| `triggerFraction` | 0,5 | `BossTurret` |
| `launchSpeed` / `spreadAngle` | 35 / 40° | `BossTurret` |
| kámen: `speed` / `stunDuration` / `sweepRadius` | 150 / 2 s / 1,5 m | `enemy_boss_rock.prefab` |

## Kde v projektu

`Assets/Prefabs/Turrets/Turret_Boss.prefab`, `Assets/3D models/enemy_boss_rock.prefab`,
`BossTurret.cs`, `ColorBall.cs`; pruh v `RunUI.cs`. Prefaby vyrábí `Tools > DroneMissile >
Build Arena` (jen pokud chybí).

## Souvisí

[waves.md](waves.md), [turret-types.md](turret-types.md), [design/waves.md](../design/waves.md),
rozhodnutí [#13](../decisions.md) (výjimka), [#23](../decisions.md).
