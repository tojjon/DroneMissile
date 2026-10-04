# Hardcore mód

**Stav:** postaveno, **čeká na kompilaci a test hraním.** Zadání a plán 04.10.2026. Po testu se
přesune do [archive/](archive/README.md).

## Zadání `[ZADÁNÍ]`

Třetí obtížnost **Hardcore**, ve které **nejsou žádné upgrady**. Dohodnuto s Viktorem 04.10.2026:

- **Pravidla jako Normal:** smrt ukončí run, stěny a strop zabíjí, stejné vlny a turrety. Jediný
  rozdíl je, že hráč nikdy nedostane karty.
- **Mezi vlnami žádná pauza:** jen **2s nápis „WAVE N CLEARED"** a další vlna začne sama. Tlak
  nepoleví.
- **Vlastní rekord** nejlepší vlny, odděleně od Easy a Normal. Obrazovka s výsledkem píše HARDCORE.

## Co už je připravené

- Obtížnost se volí v main menu a pamatuje v `GameSession.CurrentDifficulty`.
- Nejlepší vlna se ukládá **podle názvu obtížnosti** (`BestWave_<obtížnost>`), takže nová obtížnost
  dostane vlastní rekord sama.
- Stěny omráčí jen na Easy (`DroneControls`), smrt restartuje vlnu jen na Easy (`RunManager`) —
  všechno ostatní se chová jako Normal, takže Hardcore pravidla Normalu zdědí bez úprav.
- Upgrady ještě neexistují ([upgrades.md](upgrades.md)) — dnes je tedy Hardcore od Normalu poznat
  hlavně podle chybějící pauzy mezi vlnami.

## Plán implementace

1. **`GameSession`:** `Difficulty { Easy, Normal, Hardcore }` a `UpgradesEnabled` (false na
   Hardcore). Na tuhle vlastnost se bude ptát budoucí systém karet — **kód karet ji musí
   respektovat**, zapsat i do [upgrades.md](upgrades.md).
2. **`MainMenu`:** tlačítko **Hardcore** ve výběru obtížnosti pod Normal.
3. **`RunManager`:** nový stav `Banner`. Na Hardcore se po vyčištění vlny hra **nezastaví** —
   stav `Banner` na `hardcoreBannerTime` (2 s), pak `StartWave(n + 1)`. Smrt během nápisu pořád
   ukončí run. Poslední vlna dál končí výhrou.
4. **`RunUI`:** v `Banner` velký nápis „WAVE N CLEARED" uprostřed, bez tlačítek a bez kurzoru.
   Na obrazovce s výsledkem v záložce Upgrades „Hardcore — no upgrades."
5. **Dokumentace:** rozhodnutí, [features/death-and-difficulty.md](../features/death-and-difficulty.md),
   [features/main-menu.md](../features/main-menu.md),
   [features/between-wave-screen.md](../features/between-wave-screen.md),
   [design/game-structure.md](../design/game-structure.md).

## Ověření

Headless kompilace; Viktor: Main menu → New game → Hardcore → vlna 1 vyčištěná → nápis 2 s →
vlna 2 sama; smrt → RUN OVER s obtížností Hardcore a vlastním rekordem; Easy a Normal beze změny.

## Mimo rozsah

Další ztížení (víc turretů, rychlejší palba) — Viktor chce zatím jen „Normal bez upgradů".

## Souvisí

[upgrades.md](upgrades.md), [design/game-structure.md](../design/game-structure.md),
rozhodnutí [#19](../decisions.md), [#20](../decisions.md), [#23](../decisions.md).
