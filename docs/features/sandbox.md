# Sandbox

**Stav:** hotovo · otestováno hraním: ano (04.10.2026, i konzole upgradů) ·
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

## Konzole upgradů

**Enter** otevře pole vlevo dole a hra se zastaví. Napiš jméno upgradu a Enter — dron ho dostane
(další stack, pokud už ho má). Stačí začátek jména a nezáleží na velikosti písmen ani mezerách
(`dou` = Double Strike). Pod polem se vypisují upgrady, které odpovídají napsanému.

| Příkaz | Co udělá |
|---|---|
| `jméno` | přidá upgrade |
| `remove jméno` | sundá jeden stack |
| `clear` | sundá všechno |
| prázdný Enter / **Esc** | zavře konzoli (Esc tady neodchází do menu) |

Upgrady **přežijí pád** — vynuluje je až návrat do menu. Vlastněné upgrady jsou vypsané vlevo dole.
Funguje i po Hardcore runu (obtížnost v sandboxu nic neblokuje). Plán:
[plans/archive/sandbox-console.md](../plans/archive/sandbox-console.md).

## Kde v projektu

`Assets/Scenes/Sandbox.unity`. Vyrábí `Tools > DroneMissile > Build Sandbox Scene` (kopie +
rozmístění turretů + konzole); **ruční úpravy přepíše další spuštění.** Konzole: `SandboxConsole`,
do existující scény ji přidá `Tools > DroneMissile > Add Sandbox Console`.

## Souvisí

[turret-types.md](turret-types.md), [main-menu.md](main-menu.md),
[design/game-structure.md](../design/game-structure.md).
