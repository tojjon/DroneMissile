# Žluté kruhy

**Stav:** hotovo · otestováno hraním: **zatím ne** · `[OVĚŘENO 04.10.2026]`

## Co to je

Svítící žluté obruče ve vzduchu arény. **Průlet kruhem přidá +1 damage k příští raketě.** Bonus se
získává přesným letem, ne sbíráním — sedí to na pilíř „letová fyzika je základ".

## Jak se to chová

- **3 kruhy za vlnu**, náhodně 10–60 m nad podlahou, náhodně natočené.
- Bonusy se **sčítají**: dva kruhy = příští raketa +2 (12 damage).
- Vlevo nahoře svítí `NEXT SHOT +k`, dokud bonus nevystřelíš.
- **Posílená raketa bliká žlutě.**
- Proletěný kruh zmizí s žlutým zábleskem.
- **Obruč nemá collider** — dotek okraje dron nezabije. Průlet hlídá jen neviditelný trigger
  v otvoru.
- Rakety a nepřátelské projektily kruhy ignorují.

## Ladění

`ringsPerWave`, `ringMinHeight`, `ringMaxHeight` na `RunManager`; velikost obruče (`radius` 4 m)
v `YellowRing`; barva blikání `boostColor` na raketě.

## Kde v projektu

`YellowRing.cs`, `Shoting.cs` (`AddBonus`, `PendingBonus`), `RocketProjectile.cs` (blikání),
materiál `arena_ring.mat`.

## Otevřené

+1 je 10 % rakety — stačí to, nebo se má bonus škálovat? ([design/waves.md](../design/waves.md))

## Souvisí

[player-rockets.md](player-rockets.md), [waves.md](waves.md), rozhodnutí [#23](../decisions.md).
