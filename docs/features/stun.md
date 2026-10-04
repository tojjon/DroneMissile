# Stun (omráčení dronu)

**Stav:** hotovo · otestováno hraním: ano · `[OVĚŘENO 04.10.2026]`

## Co to je

Dron nemá HP. **Zásah nepřátelským projektilem místo poškození bere ovládání:** po dobu stunu
nefunguje tah ani rotace, dron drží poslední náklon a padá. Nízko nad zemí je to jistý pád, výš se
to dá vybrat ([rozhodnutí #10](../decisions.md)).

## Jak se to chová

- Délka stunu je pole na projektilu (`stunDuration`, většinou 1 s; kameny bosse 2 s).
- **I-frames:** po skončení stunu nejde dron 0,5 s znovu omráčit (`stunImmunity`) — jinak by rychle
  střílející turret dron držel bez ovládání až do pádu.
- Střílet se během stunu dá.
- Na **Easy** v aréně omráčí i náraz do stěny nebo stropu (1 s, `wallStun`) —
  viz [death-and-difficulty.md](death-and-difficulty.md).

## Dva vzhledy

| Druh | Vypadá jako | Kdo ho způsobí |
|---|---|---|
| **Electric** | modrobílé výboje kolem dronu (částice s noise a trails) | modrý turret |
| **Rock** | žluté padající úlomky (zatím žluté, „kamenný vibe") | šedý, červený, zelený, boss, stěny na Easy |

U obou svítí červený rám HUD ([hud.md](hud.md)). Efekty jsou ve world space, takže při letu
proplouvají kolem kamery.

## Kde v projektu

`DroneControls.Stun(sekundy, StunKind)`, dvě instance `DroneStunArcs` na dronu (jedna na druh).

## Souvisí

[turret-types.md](turret-types.md), rozhodnutí [#10](../decisions.md), [#16](../decisions.md),
[#17](../decisions.md), [#22](../decisions.md).
