# Vlny a boss

Obsah jednotlivých vln. Smyčka kolem nich (pauza, karty, menu) je v
[game-structure.md](game-structure.md), typy turretů v [enemies.md](enemies.md). Zdroj: Viktorův
podklad *Wavky*, 03.10.2026.

## Vlny 1–10 `[ZADÁNÍ]`

| Vlna | Obsah |
|---|---|
| 1 | **jeden** šedý turret |
| 2 | šedé turrety (víc než jeden, počet neurčen) |
| 3–9 | *zatím nenavrženo* |
| 10 | **Boss** |

## Boss (vlna 10) `[ZADÁNÍ]`

**Velký šedý turret.**

- **Health bar nahoře na obrazovce** — v HUDu, ne ve světě. Je to výjimka z
  [rozhodnutí #13](../decisions.md), které pro běžné turrety drží pruh ve world space nad turretem.
- **Silné střely.**
- **Od poloviny HP** začne střílet **nahoru, v různých směrech, barevné kuličky** v barvách typů
  turretů. Kde kulička dopadne, tam se **spawne turret té barvy**.
- **Počet kuliček je proměnná** nastavitelná v Inspectoru.

## Žluté kruhy `[ZADÁNÍ]`

V **každé vlně** se spawnují **žluté kruhy**. Když jimi dron proletí:

- **příští střela** dá **+1 damage**,
- a ta střela **bliká žlutě**, aby bylo vidět, že je posílená.

Sedí to na pilíř „letová fyzika je základ" ([concept.md](../concept.md)) — bonus se získává
přesným průletem, ne sbíráním.

## Implementace `[OVĚŘENO 04.10.2026]`

Běží ve scéně `Arena` (New game → obtížnost → Arena), řídí ji `RunManager`
([rozhodnutí #23](../decisions.md)). Tabulka vln je pole `waves` na `RunManager` a dá se měnit
v Inspectoru:

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

- **Spawn:** náhodně na podlaze, 20 m od stěn, turrety 40 m od sebe a 60 m od dronu.
- **Boss** (`Turret_Boss`): šedý turret ×3, **600 HP**, střílí **velké kameny**
  (`enemy_boss_rock`: ×3, 150 m/s, spolehlivý sweep o poloměru 1,5 m, stun **2 s**). Od **50 % HP**
  vystřelí **6 kuliček** (`ballCount`) a pak znovu každých **25 s** (`volleyInterval`), barva každé
  náhodně. Kde kulička dopadne na podlahu, vyroste turret té barvy; od stěny se odrazí. **Vlna končí,
  až padne boss i všechny přivolané turrety.** Pruh HP bosse je nahoře na obrazovce.
- **Po vlně 10 je výhra** — obrazovka s výsledkem ve variantě „VICTORY".
- **Žluté kruhy:** **3 za vlnu** (`ringsPerWave`), náhodně 10–60 m nad podlahou. Každý průlet přidá
  **+1 k příští střele, stackuje se** (dva kruhy = +2), kruh zmizí. Posílená raketa bliká žlutě,
  vlevo nahoře svítí `NEXT SHOT +k`. Kruhy samy nemají collider — dotek okraje dron nezabije.

## Vlny 11–30 `[ZADÁNÍ]`

Viktor, 06.10.2026: run pokračuje **do vlny 30**. **Vlna 20** je boss, jehož kuličky jsou všech
barev **kromě šedé**; **vlna 30** totéž, ale kuličky **jen zelené**. Obsah ostatních vln je návrh
v [plans/waves-30.md](../plans/waves-30.md) a tabulka v [features/waves.md](../features/waves.md).

## Otevřené otázky `[OTEVŘENÉ]`

1. ~~**Vlny 3–9.**~~ **ROZHODNUTO 04.10.2026** — barvy se přidávají postupně, tabulka výš.
2. ~~**Co po vlně 10?**~~ **ROZHODNUTO 04.10.2026** — výhra, run končí.
3. ~~**Kde se turrety spawnují?**~~ **ROZHODNUTO 04.10.2026** — náhodně na podlaze.
4. ~~**Boss — „silné střely".**~~ **ROZHODNUTO 04.10.2026** — velké kameny, delší stun, spolehlivé
   zásahy.
5. ~~**Boss — kuličky.**~~ **ROZHODNUTO 04.10.2026** — od poloviny HP, pak každých 25 s; barvy
   náhodně; přivolané turrety se musí zničit.
6. **Boss — kolik HP?** Zatím 600 (60 zásahů). Ladit hraním.
7. ~~**Žluté kruhy — kolik a kde?**~~ **ROZHODNUTO 04.10.2026** — 3 za vlnu, náhodně, po průletu
   zmizí.
8. ~~**Žluté kruhy — stackují se?**~~ **ROZHODNUTO 04.10.2026** — ano. Zbývá: **+1 je 10 % rakety**
   (10 dmg) — stačí to, nebo se má bonus škálovat?
