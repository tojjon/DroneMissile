# Obrazovka s výsledkem

**Stav:** hotovo · otestováno hraním: **zatím ne** · `[OVĚŘENO 04.10.2026]`

## Co to je

Konec runu. Dvě varianty:

- **RUN OVER** — smrt na Normal,
- **VICTORY** — vyčištěná vlna 10 (boss).

## Co ukazuje

Záložka **Stats**:

- obtížnost,
- dosažená vlna (z 10),
- **nejlepší vlna** — rekord přes všechny runy, s „NEW RECORD!", když padl,
- damage v poslední vlně a za celý run,
- počet zničených turretů.

Záložka **Upgrades**: zatím „No upgrades picked." — sem přijdou karty vybrané za run.

Tlačítko **Main menu**.

## Jak se to chová

Hra je pod obrazovkou zastavená. Nejlepší vlna se ukládá do `PlayerPrefs` **zvlášť pro Easy
a Normal**, takže přežije i vypnutí hry.

## Kde v projektu

`RunUI.cs` (`BuildEndScreen`), `RunManager.End()`, `GameSession` (statistiky, `BestWave`).

## Souvisí

[death-and-difficulty.md](death-and-difficulty.md), [between-wave-screen.md](between-wave-screen.md),
[design/game-structure.md](../design/game-structure.md), rozhodnutí [#23](../decisions.md).
