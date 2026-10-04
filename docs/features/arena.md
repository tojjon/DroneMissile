# Aréna

**Stav:** hotovo · otestováno hraním: ano (do vlny 2) · `[OVĚŘENO 04.10.2026]`

## Co to je

Uzavřený box, ve kterém běží run po vlnách: **300 × 300 m, 80 m vysoký, prázdný** — podlaha,
čtyři stěny, strop. Dron startuje u jižní stěny čelem do arény.

## Jak se to chová

- **Podlaha** zabíjí na obou obtížnostech.
- **Stěny a strop** na Easy jen omráčí, na Normal zabijí
  ([death-and-difficulty.md](death-and-difficulty.md)).
- Stěny a strop nevrhají stíny, aby strop nezastínil celou podlahu.
- Turrety se spawnují na náhodných místech podlahy, kruhy ve vzduchu ([waves.md](waves.md)).

## Ladění

Rozměry jsou konstanty v `ArenaBuilder` (`Width`, `Depth`, `Height`); po změně se aréna musí
znovu postavit. `RunManager` drží `floorSize`, `height` a odstupy pro spawn.

## Kde v projektu

`Assets/Scenes/Arena.unity` — kopie `SampleScene` bez terénu, plošiny a turretu, s boxem. Vyrábí
`Tools > DroneMissile > Build Arena`; **ruční úpravy scény přepíše další spuštění.** Materiály
`arena_floor.mat`, `arena_wall.mat`.

## Zbývá

Kryty / překážky uvnitř (zatím záměrně prázdná).

## Souvisí

[waves.md](waves.md), [concepts/arena-run.md](../concepts/arena-run.md),
rozhodnutí [#20](../decisions.md), [#23](../decisions.md).
