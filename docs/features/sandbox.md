# Sandbox

**Stav:** hotovo (bez konzole upgradů) · otestováno hraním: ano (04.10.2026) ·
`[OVĚŘENO 04.10.2026]`

## Co to je

Volný svět bez vln na zkoušení. Je to kopie původní mapy (`SampleScene`): terén 3 × 3 km,
plovoucí plošina, dron.

## Co v něm je

**Po jednom turretu každého typu, 600 m od plošiny** do čtyř světových stran:

| Směr | Typ |
|---|---|
| sever | šedý |
| východ | modrý |
| jih | červený |
| západ | zelený |

Turret vidí jen do 360 m, takže na plošině tě nic neohrožuje a ke každému typu se letí zvlášť —
nikdy nestřílí dva najednou.

Pád znovu načte sandbox. **Esc** vrací do menu.

## Zbývá

**Konzole upgradů:** Enter → napsat jméno upgradu → dron ho dostane. Čeká na systém upgradů.

## Kde v projektu

`Assets/Scenes/Sandbox.unity`. Vyrábí `Tools > DroneMissile > Build Sandbox Scene` (kopie +
rozmístění turretů); **ruční úpravy přepíše další spuštění.**

## Souvisí

[turret-types.md](turret-types.md), [main-menu.md](main-menu.md),
[design/game-structure.md](../design/game-structure.md).
