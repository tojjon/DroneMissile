# Konzole upgradů v sandboxu

**Stav:** plánováno — čeká na systém upgradů ([upgrades.md](upgrades.md)). Sandbox sám existuje
([features/sandbox.md](../features/sandbox.md)).

## Co to má být `[ZADÁNÍ]`

V sandboxu se po stisku **Enter** otevře textové pole. Hráč napíše **jméno upgradu** a dron ho
dostane. Smysl: vyzkoušet si upgrady a jejich kombinace, aniž by je musel vyhrát v runu.

## Návrh implementace

- Pole se staví v kódu stejně jako ostatní UI (`UiKit`), hra se při psaní zastaví nebo aspoň
  přestane číst ovládání z klávesnice (jinak by WASD pohnulo dronem).
- Jména se hledají ve stejném seznamu upgradů jako karty a katalog; při psaní by šlo napovídat.
- Neznámé jméno → krátká hláška, nic se nestane.

## Otevřené otázky `[OTEVŘENÉ]`

1. **Jak se upgrade odebírá?** Příkaz (např. „remove …"), reset všech, nebo restart sandboxu?
2. **Seznam dostupných jmen** — vypsat na obrazovku, nebo jen napovídat při psaní?
3. **Přežijí upgrady pád** v sandboxu (pád znovu načte scénu)?

## Souvisí

[upgrades.md](upgrades.md), [design/game-structure.md](../design/game-structure.md).
