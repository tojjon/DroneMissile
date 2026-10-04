# Vlny

**Stav:** hotovo · otestováno hraním: vlny 1–2 ano, 3–10 zatím ne · `[OVĚŘENO 04.10.2026]`

## Co to je

Run v [aréně](arena.md) má **10 vln**. Vlna začne spawnem turretů a žlutých kruhů a skončí, až jsou
všechny turrety zničené. Pak se hra zastaví na [obrazovce mezi vlnami](between-wave-screen.md).
Desátá vlna je [boss](boss.md); po ní je výhra.

| Vlna | Šedý | Modrý | Červený | Zelený |
|---|---|---|---|---|
| 1 | 1 | | | |
| 2 | 2 | | | |
| 3 | 1 | 1 | | |
| 4 | 2 | 1 | | |
| 5 | 2 | | 1 | |
| 6 | 1 | 1 | 1 | |
| 7 | 2 | | | 1 |
| 8 | 1 | 1 | 1 | 1 |
| 9 | 2 | 2 | 2 | 2 |
| 10 | **boss** | | | |

Vlevo nahoře svítí `WAVE n / 10`.

## Jak se to chová

- Turrety se objeví na **náhodných místech podlahy**: 20 m od stěn, 40 m od sebe, nejméně 60 m od
  dronu.
- V každé vlně jsou 3 [žluté kruhy](yellow-rings.md).
- Na Easy smrt vlnu restartuje (nové pozice), na Normal končí run
  ([death-and-difficulty.md](death-and-difficulty.md)).
- Nová vlna smaže všechno, co z minulé ještě letí.

## Ladění

Tabulka je pole **`waves`** na objektu **`RunManager`** ve scéně `Arena` — počty se mění přímo
v Inspectoru, řádky jdou přidávat i přesouvat. **Poslední řádek je finální vlna**; vyčištění = výhra.
Tip na test bosse: přetáhni řádek s bossem na začátek (a pak zpátky).

Další pole: `wallMargin`, `minTurretSpacing`, `minDistanceFromDrone`, `ringsPerWave`.

## Kde v projektu

`Assets/scripts/RunManager.cs`, `RunUI.cs`; statistiky v `GameSession.cs`.

## Souvisí

[design/waves.md](../design/waves.md), [concepts/arena-run.md](../concepts/arena-run.md),
rozhodnutí [#23](../decisions.md).
