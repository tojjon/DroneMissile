# Efekty zásahů a výbuchů

**Stav:** hotovo · otestováno hraním: ano · `[OVĚŘENO 04.10.2026]`

## Co to je

Krátké výbuchy postavené z částic **v kódu** — žádné prefaby ani assety efektů
([rozhodnutí #16](../decisions.md)). Jeden komponent `ImpactExplosion` (záblesk, kužel jisker,
krátké světlo) se přebarvuje a škáluje podle toho, kdo ho použije.

## Kde se objevuje

| Kdo | Barva | Velikost |
|---|---|---|
| raketa hráče — každý dopad | červená | 1 |
| modrý turret — dopad náboje | modrá | 0,6 |
| červený turret — výbuch granátu | oranžovočervená | podle `blastRadius` |
| zelená raketa — vyhoření | zelená | 0,4 |
| žlutý kruh — průlet | žlutá | 1,5 |

Elektrický výboj modrého turretu (`ElectricZap`) je samostatný částicový efekt — blesk z místa
dopadu k dronu.

## Na co pozor

- Barva je **HDR a jde přes materiál**, ne přes barvu částice — jinak by se ořízla na 1 a efekt by
  nezářil v Bloomu ([rozhodnutí #17](../decisions.md)).
- Každý efekt se sám zničí po ~0,5 s.

## Kde v projektu

`Assets/scripts/ImpactExplosion.cs`, `ElectricZap.cs`, sdílené materiály v `FxAssets.cs`.

## Souvisí

[stun.md](stun.md), [reference/unity-gotchas.md](../reference/unity-gotchas.md),
rozhodnutí [#16](../decisions.md), [#17](../decisions.md).
