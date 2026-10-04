# Obrazovka mezi vlnami

**Stav:** hotovo (bez karet) · otestováno hraním: ano · `[OVĚŘENO 04.10.2026]`

## Co to je

Po vyčištění vlny se hra **zastaví** a ukáže panel:

- titulek **„WAVE N CLEARED"**,
- damage v této vlně, damage za celý run, počet zničených turretů,
- **tři sloty na karty** s textem „No upgrades yet",
- tlačítko **Continue** → další vlna.

Ovládá se myší nebo Enterem. Během pauzy stojí fyzika i střelba; dron po Continue pokračuje tam,
kde byl.

## Zbývá

**Karty upgradů** — podle zadání 5 náhodných karet s raritou (common 60 % … mythic 1 %), hráč si
jednu vybere. Sloty jsou připravené, čeká se na seznam upgradů
([design/game-structure.md](../design/game-structure.md)).

## Kde v projektu

`RunUI.cs` (`BuildIntermission`), stav `Intermission` v `RunManager.cs`.

## Souvisí

[waves.md](waves.md), [end-screen.md](end-screen.md), rozhodnutí [#23](../decisions.md).
