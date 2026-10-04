# Obrazovka mezi vlnami

**Stav:** hotovo · otestováno hraním: obrazovka ano, karty zatím ne · `[OVĚŘENO 04.10.2026]`

## Co to je

Po vyčištění vlny se hra **zastaví** a ukáže panel:

- titulek **„WAVE N CLEARED"**,
- damage v této vlně, damage za celý run, počet zničených turretů,
- **karty upgradů** (až 5, bez opakování) — **klik na kartu = upgrade + další vlna**
  ([upgrades.md](upgrades.md)),
- tlačítko **Continue** jen tehdy, když není co nabídnout.

Na **Hardcore** se tahle obrazovka neukazuje: místo ní 2 s nápis „WAVE N CLEARED" a další vlna
začne sama, bez pauzy ([rozhodnutí #25](../decisions.md)).

Ovládá se myší nebo Enterem. Během pauzy stojí fyzika i střelba; dron po Continue pokračuje tam,
kde byl.

## Kde v projektu

`RunUI.cs` (`BuildIntermission`), stav `Intermission` v `RunManager.cs`.

## Souvisí

[waves.md](waves.md), [end-screen.md](end-screen.md), rozhodnutí [#23](../decisions.md).
