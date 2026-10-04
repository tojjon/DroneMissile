# Smrt a obtížnost

**Stav:** hotovo · otestováno hraním: Normal ano (do vlny 2), Easy restart zatím ne ·
`[OVĚŘENO 04.10.2026]`

## Co to je

Dron nemá HP — **dotek čehokoli pevného je pád** (terén, podlaha, turret, přistání taky). Co se
stane potom, záleží na tom, kde hráč je a jakou obtížnost zvolil v menu.

## Obtížnost (volí se při New game)

| | Easy | Normal |
|---|---|---|
| **Pád** (podlaha, turret) | vlna se **restartuje** — dron zpět na spawn, turrety a kruhy znovu, statistiky runu zůstávají | **konec runu** → [obrazovka s výsledkem](end-screen.md) |
| **Stěna nebo strop arény** | jen **omráčí** (1 s) | zabije |

Po pádu se na 1 s ukáže „YOU DIED" (`deathDelay`), teprve potom se stane restart nebo konec.

## Mimo arénu

V `SampleScene` a `Sandbox` platí původní chování: pád **znovu načte scénu**
([rozhodnutí #10](../decisions.md)).

## Pojistky v kódu, které vypadají zbytečně

- **Arming latch (`armAltitude` 0,5 m):** dron startuje položený na zemi; pád se počítá až potom,
  co vystoupá nad spawn. Jinak by se smrt spustila v čase 0.
- **Kill plane (`killY`):** pád pod tuhle výšku je smrt i bez kolize (−70 m na terénu, −20 m
  v aréně).

## Kde v projektu

`DroneControls` (`reloadSceneOnDeath`, `wallStun`, `ResetTo`), `RunManager` (rozhoduje v aréně),
`ArenaWall` (značka na stěnách a stropu), `GameSession.CurrentDifficulty`.

## Souvisí

[stun.md](stun.md), [arena.md](arena.md), [design/game-structure.md](../design/game-structure.md),
rozhodnutí [#10](../decisions.md), [#19](../decisions.md), [#20](../decisions.md),
[#23](../decisions.md).
