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

Záložka **Upgrades**: karty vybrané za run se stupněm (×2, ×3); bez karet „No upgrades
picked.", na Hardcore „Hardcore - no upgrades."

Tlačítko **Main menu**.

## Jak se to chová

Hra je pod obrazovkou zastavená. Nejlepší vlna se ukládá do `PlayerPrefs` **zvlášť pro Easy,
Normal a Hardcore**, takže přežije i vypnutí hry.

## Kde v projektu

`RunUI.cs` (`BuildEndScreen`), `RunManager.End()`, `GameSession` (statistiky, `BestWave`).

## Souvisí

[death-and-difficulty.md](death-and-difficulty.md), [between-wave-screen.md](between-wave-screen.md),
[design/game-structure.md](../design/game-structure.md), rozhodnutí [#23](../decisions.md).
