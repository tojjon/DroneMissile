# Plán: Aréna, vlny 1–10 + boss, žluté kruhy, obrazovky mezi vlnami a na konci

**Plán ze session 04.10.2026.** Stav: **implementováno** (commit `0db0288`). Hraním otestováno
po vlnu 2; boss, žluté kruhy, Easy restart a obrazovka s výsledkem zatím ne. Hotová podoba je ve
[features/](../../features/) (aréna, vlny, boss, kruhy, obrazovky, obtížnost). Rozhodnutí je
[#23](../../decisions.md).

## Kontext

Předělání ([#18](../../decisions.md)–[#20](../../decisions.md),
[design/game-structure.md](../../design/game-structure.md), [design/waves.md](../../design/waves.md))
dělá ze hry run po vlnách v uzavřeném boxu. Menu a prefaby turretů už existovaly, ale New game
pořád vedl do `SampleScene`. Tenhle plán staví samotný run.

## Dohodnuto s Viktorem

- **Aréna:** 300 × 300 m, 80 m vysoká, **prázdná** (podlaha, 4 stěny, strop).
- **Vlny 3–9:** barvy se přidávají postupně (tabulka níž), měnitelné v Inspectoru.
- **Boss (vlna 10):** velký šedý turret, pruh HP nahoře na obrazovce, střílí **velké kameny**
  (větší, delší stun, spolehlivé zásahy). Od 50 % HP vystřelí salvu barevných kuliček a **pak
  každých 25 s**; kulička spawne turret své barvy tam, kam dopadne. **Vlna končí, až padne boss
  i všechny přivolané turrety.**
- **Po bossovi výhra** → obrazovka s výsledkem ve variantě „VICTORY".
- **Žluté kruhy:** 3 za vlnu náhodně; každý průlet přidá +1 k příští střele (**stackuje se**);
  posílená raketa bliká žlutě.
- **Mezi vlnami:** pauza, „Wave N cleared", sloty na karty s „No upgrades yet" a Continue.
- **Smrt** ([#19](../../decisions.md), [#20](../../decisions.md)): Easy = restart vlny, stěny a strop jen
  omráčí; Normal = konec runu, stěny zabijí. Podlaha a turrety zabíjí na obou.
- **Obrazovka s výsledkem:** dosažená vlna, nejlepší vlna (uložená), damage ve vlně i za run,
  zničené turrety a záložka Upgrades (placeholder). Tlačítko Main menu.

## Tabulka vln

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

## Přístup

1. **Stav runu — `GameSession`:** `GameScene = "Arena"`, statistiky runu, nejlepší vlna
   v `PlayerPrefs` zvlášť pro každou obtížnost.
2. **Smrt a stěny — `DroneControls`, `DroneHUD`:** `reloadSceneOnDeath` (v aréně vypnuté — o smrti
   rozhoduje `RunManager`); značka `ArenaWall` na stěnách a stropu (komponenta, ne tag); na Easy
   náraz do `ArenaWall` omráčí; `ResetTo()` pro restart vlny; HUD smaže hlášku smrti po resetu.
3. **Damage, kruhy, blikání:** `RocketProjectile.damage` (10) místo zadrátované hodnoty; `Shoting`
   drží bonus z kruhů a nepálí v pauze; `EnemyTurret` hlásí udělený damage; `YellowRing` je obruč
   **bez colliderů** s triggerem přes otvor.
4. **Run — `RunManager`:** spawn turretů na náhodných místech podlahy (odstup od stěn, od sebe, od
   dronu), kruhy, detekce vyčištěné vlny, pauza, smrt podle obtížnosti, výhra po poslední vlně.
5. **Boss — `BossTurret` + `ColorBall`:** `Turret_Boss` (šedý ×3, 600 HP, bez health baru nad
   sebou) střílí `enemy_boss_rock`; salvy kuliček; kulička je balistická, na podlaze spawne turret
   a zaregistruje ho, od stěny se odrazí.
6. **UI — `UiKit` + `RunUI`:** sdílená stavebnice z `MainMenu`; `RunUI` ukazuje vlnu, bonus,
   pruh bosse, obrazovku mezi vlnami a RUN OVER / VICTORY.
7. **Builder — `ArenaBuilder`:** scéna `Arena` jako kopie `SampleScene` bez terénu, s boxem;
   prefaby bosse; build list `MainMenu, Arena, SampleScene, Sandbox`.
8. **Dokumentace:** rozhodnutí #23, design dokumenty, CLAUDE.md.

## Ověření

Headless kompilace a build, kontrola zapojení; Viktor hraje Normal (vlny, smrt o stěnu, RUN OVER),
Easy (stun o stěnu, restart vlny), kruhy (+2 a blikání), vlnu 10 (pruh, kameny, salvy, VICTORY).

## Mimo rozsah

Skutečné karty upgradů, ukládání / Continue, ESC pauza, vlny po desáté.

## Odchylky při implementaci

- Projektily (raketa hráče i nepřátelské) **ignorují triggery** v `OnTriggerEnter` — jinak by
  vybuchovaly o otvory kruhů.
- `EnemyProjectile` dostal `sweepRadius` (sphere cast), aby velké kameny bosse trefovaly podle své
  velikosti, ne jen středovou čárou.
- `ColorBall.InFlight` — vlna se nevyhodnotí jako vyčištěná, dokud letí kuličky.
- Stěny a strop nevrhají stíny, jinak by strop zastínil celou podlahu.
- `MainMenuBuilder` kopíruje `SampleScene` napevno — `GameSession.GameScene` už ukazuje na arénu.
