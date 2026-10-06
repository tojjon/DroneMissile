# Upgrady a karty

**Stav:** postaveno · otestováno hraním: **zatím ne** · `[OVĚŘENO 06.10.2026]`

## Co to je

Po každé vyčištěné vlně (mimo Hardcore) hra nabídne **karty upgradů**. Hráč jednu vybere —
**klik na kartu dá upgrade a spustí další vlnu**. Stejný upgrade se dá vzít víckrát; stupně se
sčítají.

- Nabídka má **až 5 karet** (`cardsOffered` na `RunUI`), **nikdy dvakrát tutéž**. Upgradů je 8,
  takže se ukazuje plných 5.
- Rarita se losuje podle vah (Common 60 %, Uncommon 20 %, Rare 10 %, Epic 6 %, Legendary 3 %,
  Mythic 1 %); rarita, ve které nic nezbývá, se losuje znovu.
- Raritu ukazuje **barva rohů karty**.
- Upgrady vydrží restart vlny na Easy; nový run (návrat do menu) je vynuluje. Na **Hardcore** nejsou.

## Upgrady

| Karta | Rarita | Co dělá | Další karta |
|---|---|---|---|
| ![Double Strike](../../Assets/UI/Upgrades/double_strike.png) **Double Strike** | Common | stisk = dávka raket rychle za sebou (0,1 s); bonus ze žlutých kruhů dostane každá | +1 raketa |
| ![Lightning](../../Assets/UI/Upgrades/lightning.png) **Lightning** | Rare | po zásahu turretu skočí modrý blesk na nejbližší další turret do 160 m za polovinu damage | +1 skok (vždy na nový turret) |
| ![Homing](../../Assets/UI/Upgrades/homing.png) **Homing** | Epic | na HUD velký čtverec; turret uvnitř zčervená čtverec a vystřelená raketa se na něj navede | větší čtverec + ostřejší zatáčení |
| ![+1 Damage](../../Assets/UI/Upgrades/damage.png) **+1 Damage** | Common | +1 damage každé raketě dávky (a každému tiku ohně); raketa kvůli tomu nebliká | +1 damage |
| ![Block](../../Assets/UI/Upgrades/block.png) **Block** | Common | **F** = 1 s štít: žádný projektil drona neomráčí, okraj obrazovky zežloutne; nabití (žluté body pod zaměřovačem) se doplní po 8 s | +1 nabití |
| ![Bouncy](../../Assets/UI/Upgrades/bouncy.png) **Bouncy** | Uncommon | raketa se odrazí od podlahy, stěn, stropu i terénu (s jiskrou); turret je vždy zásah | +1 odraz |
| ![Fire](../../Assets/UI/Upgrades/fire.png) **Fire** | Rare | zasažený turret hoří 5 s, každou 1 s ztratí 2 HP; další zásah oheň obnoví | +5 s |
| ![Freeze](../../Assets/UI/Upgrades/freeze.png) **Freeze** | Rare | zasažený turret zmodrá a 2 s se neotáčí ani nestřílí; bossovi stojí i salva koulí | +2 s |

Fire i Freeze platí pro každou raketu dávky i pro **skoky Lightning**. Block chrání jen před
projektily — stěna arény na Easy omráčí i během bloku.

Homing je **záměrná výjimka** z pravidla, že raketa letí přesně na střed zaměřovače
([rozhodnutí #12](../decisions.md), [#26](../decisions.md)) — platí jen s kartou.

## Ladění

Na komponentě `Shoting` (dron), sekce *Upgrade*:

| Pole | Výchozí |
|---|---|
| `burstInterval` | 0,1 s |
| `homingScopeBase` / `homingScopePerStack` / `homingScopeMax` | 25 % / +10 % / 80 % výšky obrazovky |
| `homingTurnBase` / `homingTurnPerStack` | 90 °/s / +60 °/s |
| `homingRange` | 300 m |
| `chainRange` | 160 m (Viktor, 04.10.2026; původně 40 m) |
| `chainDamageFraction` | 0,5 |
| `fireDurationPerStack` / `fireTickDamage` / `fireTickInterval` | 5 s / 2 / 1 s |
| `freezeDurationPerStack` | 2 s |

Na `DroneControls`: `blockDuration` 1 s, `blockRecharge` 8 s. Na `RocketProjectile`:
`bounceFxScale` 0,4. Barvy ohně a mrazu na `TurretStatusFx`, barvy bloku na `DroneHUD`.

Jména, popisy, obrázky a rarity jsou v assetech `Assets/Resources/Upgrades/*.asset`.

## Kde to je vidět

- **Mezi vlnami:** karty (3× zvětšený pixel art), myš nebo šipky + Enter.
- **Main menu → Upgrades:** katalog všech karet s raritou a popisem.
- **Obrazovka s výsledkem → Upgrades:** karty vybrané za run se stupněm (×2, ×3).

## Přidání nového upgradu

1. Obrázek karty (71 × 100) do `Assets/UI/Upgrades/<id>.png`, rarita barvou rohů.
2. Záznam do `UpgradesBuilder` a spustit `Tools > DroneMissile > Build Upgrades` — nebo vytvořit
   asset ručně přes *Create > DroneMissile > Upgrade*.
3. Efekt naprogramovat podle `id` (dnes v `Shoting` a `RocketProjectile`).

Karty, katalog i konec runu ho pak najdou samy.

## Kde v projektu

`UpgradeDefinition.cs` (asset + rarity), `UpgradeCatalog.cs` (seznam, losování),
`GameSession.cs` (vlastněné upgrady), `Shoting.cs`, `RocketProjectile.cs` (odraz, oheň/mráz při
zásahu), `EnemyTurret.cs` (hoření, zmrazení), `TurretStatusFx.cs` (plameny, modrý tón),
`DroneControls.cs` (Block), `BossTurret.cs`, `DroneHUD.cs` (čtverec, žlutý okraj, nabití),
`RunUI.cs`, `MainMenu.cs`, `UiKit.cs` (karty), `Assets/Editor/UpgradesBuilder.cs`.

## Souvisí

[plans/upgrades.md](../plans/upgrades.md), [plans/upgrades-2.md](../plans/upgrades-2.md), [between-wave-screen.md](between-wave-screen.md),
[end-screen.md](end-screen.md), [main-menu.md](main-menu.md), rozhodnutí [#26](../decisions.md), [#28](../decisions.md).
