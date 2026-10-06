# Vlny do 30, boss na 20 a 30

**Stav:** postaveno, **čeká na test hraním.**

## Co to má být `[ZADÁNÍ]`

Viktor, 06.10.2026:

> can you add waves to wave number 30 on wave number 20 the boss will shoot out all balls of color
> but not grey. on wave 30 same but just green

- Run nekončí vlnou 10, ale **vlnou 30**. Výhra = vyčištěná vlna 30.
- **Vlna 20:** boss jako na vlně 10, ale jeho barevné kuličky jsou **jen modré, červené a zelené**
  (žádná šedá).
- **Vlna 30:** boss, kuličky **jen zelené**.
- Vlna 10 zůstává, jak je (kuličky všech čtyř barev).

## Co už je připravené

- Tabulka vln je pole `RunManager.waves`, ale její hodnoty jsou **uložené ve scéně `Arena.unity`**
  — změna výchozí hodnoty v C# se do scény nedostane (CLAUDE.md: hodnoty ve scéně přebíjí kód).
- `ArenaBuilder` tabulku nenastavuje, bere výchozí z kódu; přestavba Arény by ale znovu zkopírovala
  celou scénu ze SampleScene.
- Výhra už je „poslední vlna tabulky" (`RunManager`), boss bar v `RunUI` čte `RunManager.Boss` —
  nic nemá vlnu 10 natvrdo.
- `BossTurret.Volley()` dnes losuje barvu z celého `turretPrefabs` (šedá, modrá, červená, zelená).

## Plán implementace

### Dohodnuto / výchozí návrh

Obsah vln 11–19 a 21–29 zadání neurčuje. Návrh: turretů pomalu přibývá, po bossovi krátký oddech.

| Vlna | Šedý | Modrý | Červený | Zelený | Celkem |
|---|---|---|---|---|---|
| 11 | 3 | 1 | 1 | 1 | 6 |
| 12 | 2 | 2 | 1 | 1 | 6 |
| 13 | 2 | 1 | 2 | 1 | 6 |
| 14 | 2 | 1 | 1 | 2 | 6 |
| 15 | 3 | 2 | 2 | 1 | 8 |
| 16 | 2 | 2 | 2 | 2 | 8 |
| 17 | 3 | 2 | 2 | 2 | 9 |
| 18 | 2 | 3 | 3 | 2 | 10 |
| 19 | 3 | 3 | 3 | 3 | 12 |
| **20** | **boss** — kuličky modrá/červená/zelená | | | | |
| 21 | 2 | 2 | 2 | 2 | 8 |
| 22 | 3 | 2 | 2 | 2 | 9 |
| 23 | 2 | 3 | 2 | 3 | 10 |
| 24 | 3 | 3 | 3 | 2 | 11 |
| 25 | 3 | 3 | 3 | 3 | 12 |
| 26 | 4 | 3 | 3 | 3 | 13 |
| 27 | 3 | 4 | 3 | 4 | 14 |
| 28 | 4 | 4 | 4 | 3 | 15 |
| 29 | 4 | 4 | 4 | 4 | 16 |
| **30** | **boss** — kuličky jen zelené | | | | |

Boss na 20 a 30 je **tentýž** (600 HP, stejné střely) — liší se jen barvami kuliček.

### Kód

- `RunManager.Wave`: nové pole **`bossBalls`** — `[Flags]` maska barev (`Grey`, `Blue`, `Red`,
  `Green`). **0 = všechny**, takže stará data ve scéně (vlna 10) se chovají jako dřív.
  Tabulka se přesune do statické `RunManager.DefaultWaves()`, aby ji šlo zapsat i z editoru.
- `StartWave`: po spawnu bosse mu předá masku (`BossTurret.ballColours`).
- `BossTurret.Volley`: losuje jen z povolených barev (pořadí `turretPrefabs` = šedá, modrá,
  červená, zelená — stejné jako maska).

### Zápis do scény — `Tools > DroneMissile > Apply Wave Table`

Nový editor příkaz v `ArenaBuilder` (bez přestavby scény): otevře `Arena.unity`, na `RunManager`
nastaví `waves = DefaultWaves()` a scénu uloží. Headless:
`-executeMethod ArenaBuilder.ApplyWaveTable`. Přestavba Arény dostane tabulku sama (výchozí z kódu).

### Soubory

`RunManager.cs`, `BossTurret.cs`, `Assets/Editor/ArenaBuilder.cs`, `Arena.unity` (přes příkaz),
dokumentace (`design/waves.md`, `features/waves.md`, `features/boss.md`, `decisions.md`, CLAUDE.md).

### Ověření

1. Headless kompilace + `ApplyWaveTable`; ve scéně 30 vln, 20 a 30 s maskou.
2. Viktor: vlna 10 jako dřív; vlna 20 — žádná šedá kulička; vlna 30 — jen zelené; výhra až po 30.
   Tip na rychlý test: dočasně v Inspectoru Arény zkrátit tabulku.

## Odchylky při implementaci

- Tabulka zapsaná do `Arena.unity` headless (`ApplyWaveTable`) 06.10.2026: 30 vln, maska vlny 20
  = 14 (modrá|červená|zelená), vlny 30 = 8 (zelená). Unity při uložení zároveň dopsalo do scény
  nová pole upgradů (Block, Fire, Freeze) s výchozími hodnotami a smazalo zastaralé
  `RunUI.cardColor`.

## Otevřené otázky `[OTEVŘENÉ]`

1. **Obsah vln 11–19 a 21–29** — návrh výš, ladit hraním.
2. **Má být boss na 20 a 30 silnější** (víc HP, víc kuliček, kratší interval)? Zatím stejný.
3. **Best wave a statistiky** už pracují s jakýmkoli číslem vlny — beze změny.

## Souvisí

[design/waves.md](../design/waves.md), [features/waves.md](../features/waves.md),
[features/boss.md](../features/boss.md), [decisions.md](../decisions.md) #23.
