# Ukládání a Continue

**Stav:** plánováno. Tlačítko **Continue** v main menu existuje, ale je **zašedlé**
(`GameSession.HasSave` vrací vždy false). Ukládá se zatím jen nejlepší vlna (`PlayerPrefs`).

## Co to má být `[ZADÁNÍ]`

**Continue** pokračuje v rozehrané hře — jen pokud nějaká existuje.

## Co se musí uložit

- obtížnost,
- číslo vlny,
- vlastněné upgrady ([upgrades.md](upgrades.md)),
- statistiky runu (damage, zničené turrety) — aby obrazovka s výsledkem seděla.

Stav uprostřed vlny (pozice turretů, letící projektily) ukládat **nemá smysl** — Continue začne
vlnu od začátku.

## Návrh implementace

- Uložit při vstupu na obrazovku mezi vlnami (po výběru karty, až budou karty), do souboru
  v `Application.persistentDataPath` (JSON) nebo `PlayerPrefs`.
- Smazat při konci runu (RUN OVER / VICTORY) — mrtvý run se nedá pokračovat.
- `HasSave` čte, jestli uložený run existuje; Continue ho načte do `GameSession` a otevře arénu
  na uložené vlně.

## Otevřené otázky `[OTEVŘENÉ]`

1. **Kdy se ukládá?** Po každé vlně (po výběru karty), nebo kdykoli?
2. **Uložit při odchodu do menu** přes ESC menu?
3. **Jeden slot**, nebo víc rozehraných runů? New game přepíše uložený run — zeptat se předtím?

## Souvisí

[design/game-structure.md](../design/game-structure.md), [features/main-menu.md](../features/main-menu.md),
[esc-menu.md](esc-menu.md).
