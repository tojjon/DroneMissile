# Upgrady a karty

**Stav:** postaveno, **čeká na test hraním.** Hotová podoba: [features/upgrades.md](../features/upgrades.md).
Po testu se plán přesune do [archive/](archive/README.md).

## Co to má být `[ZADÁNÍ]`

Po každé vyčištěné vlně se hra zastaví a nabídne **5 náhodně vybraných karet**. Hráč si **jednu**
vybere, dron upgrade dostane a začne další vlna. Každá karta má raritu losovanou podle vah:

| Rarita | Barva | Šance |
|---|---|---|
| Common | šedá | 60 % |
| Uncommon | zelená | 20 % |
| Rare | modrá | 10 % |
| Epic | fialová | 6 % |
| Legendary | *neurčeno* (návrh: oranžová / zlatá) | 3 % |
| Mythic | *neurčeno* (návrh: červená) | 1 % |

Stejný seznam upgradů čte několik míst ve hře:

| Kde | Co ukazuje | Dnes |
|---|---|---|
| obrazovka mezi vlnami | 5 karet na výběr | 5 prázdných slotů „No upgrades yet" |
| Main menu → Upgrades | katalog všech upgradů: obrázek + popis | „No upgrades yet." |
| ESC menu → Upgrades | upgrady, které dron právě má | ESC menu neexistuje, viz [esc-menu.md](esc-menu.md) |
| obrazovka s výsledkem → Upgrades | karty vybrané za run | „No upgrades picked." |
| Sandbox | napsat jméno → dron upgrade dostane | neexistuje, viz [sandbox-console.md](sandbox-console.md) |

## První upgrady `[ZADÁNÍ]`

Zdroj: Viktorův podklad *Upgrades* a návrhy karet, 04.10.2026. Upgrady se **sčítají** — každá další
stejná karta přidá jeden stupeň. Karty jsou pixel art 71 × 100 px v `Assets/UI/Upgrades/`.

| Karta | Upgrade | Rarita | Text na kartě |
|---|---|---|---|
| ![Double Strike](../../Assets/UI/Upgrades/double_strike.png) | **Double Strike** | Common | „Fires another shot after the first one" · +1 SHOT |
| ![Homing](../../Assets/UI/Upgrades/homing.png) | **Homing** | Epic | „Homes onto turets in scope" · +1 HOMING |
| ![Lightning](../../Assets/UI/Upgrades/lightning.png) | **Lightning** | Rare | „Chains between turets" · +1 CHAIN |

### Double Strike

- Jeden stisk výstřelu vystřelí **2 rakety místo 1**; se dvěma kartami 3, atd. (`1 + stupeň`).
- Rakety letí **rychle za sebou** (krátká dávka, ~0,1 s mezi raketami) z téhož ústí — ne vedle sebe.
- Bonus ze [žlutých kruhů](../features/yellow-rings.md) dostane **každá raketa dávky** (Viktor,
  04.10.2026); spotřebuje se až celou dávkou.

### Homing

- Na obrazovce přibude **druhý zaměřovač: velký čtverec** kolem středu.
- Když hráč vystřelí a **turret je uvnitř čtverce**, raketa se na něj navede — jako homing munice
  ve hře *Satisfactory*.
- **Každá další karta:** **větší čtverec** *a* **ostřejší zatáčení** rakety.
- Dnes je střed obrazovky přesný bod dopadu ([rozhodnutí #12](../decisions.md) vylučuje
  navádění a lead). **Homing je výjimka, kterou Viktor potvrdil 04.10.2026** — platí jen s kartou;
  bez ní platí #12 dál.

### Lightning

- Když raketa trefí turret, **přeskočí z něj výboj na nejbližší další turret do 40 m** a dá mu
  **polovinu damage** rakety.
- **Každá další karta = o jeden skok víc**, vždy na turret, který v řetězu ještě nebyl.
- Dosah a podíl damage jsou proměnné k ladění. Vizuál: částicový blesk jako `ElectricZap`.

### Rarita na kartě

Raritu ukazuje **barva v rozích karty**, ne barva ikony. Barvy z Viktorových karet (RGB) — UI hry
má použít stejné:

| Rarita | Barva rohů | Karta |
|---|---|---|
| Common | šedá `163, 163, 163` | Double Strike |
| Rare | modrá `0, 30, 255` | Lightning |
| Epic | fialová `157, 0, 255` | Homing |

Uncommon, Legendary a Mythic zatím na žádné kartě nejsou, takže jejich přesná barva čeká na první
kartu té rarity.

## Plán implementace

### Dohodnuto s Viktorem (04.10.2026)

- Rarita karty = **barva rohů** (tabulka výš).
- Losování: nejdřív rarita podle vah, **prázdná rarita se losuje znovu**.
- **Žádné duplikáty v jedné nabídce.** Dokud existují jen 3 upgrady, nabídka má 3 karty; s dalšími
  upgrady naroste až na 5 (`cardsOffered`).
- Stejný upgrade v další vlně ano — **stupně se sčítají**.
- Double Strike: dávka rychle za sebou; ring bonus dostane každá raketa dávky.
- Homing: větší čtverec **i** ostřejší zatáčení za stupeň; výjimka z [#12](../decisions.md).
- Lightning: skok do 40 m, polovina damage rakety, +1 skok za stupeň.
- Hardcore: žádná nabídka ([hardcore-mode.md](hardcore-mode.md)).

### Data — přidání upgradu = jeden asset

- **`UpgradeDefinition`** (`ScriptableObject`): `id` (to, co se bude psát v sandboxu), jméno,
  popis, obrázek karty (`Sprite`), rarita. Assety v `Assets/Resources/Upgrades/` — hra je najde
  sama (`Resources.LoadAll`), žádný seznam v kódu ani v menu.
- **`UpgradeRarity`**: rarity, váhy 60/20/10/6/3/1 a barvy rohů z karet.
- **`GameSession`**: vlastněné upgrady (`id` → stupeň, v pořadí výběru), `Stacks(id)`,
  `AddUpgrade(id)`; vynuluje se s novým runem, restart vlny na Easy je nechá.
- **`UpgradeCatalog`**: načte všechny definice; `Draw(n)` losuje nabídku podle pravidel výš.

### Karty mezi vlnami — `RunUI`

- Placeholdery se nahradí **kartami jako tlačítky**: obrázek karty ve 3× zvětšení (pixel art,
  bez rozmazání), pod ním jméno; výběr myší nebo šipkami + Enter.
- **Klik na kartu = upgrade + další vlna.** Continue zůstane jen pro případ, že není co nabídnout.
- Na Hardcore se nabídka vůbec neukáže (už dnes jde rovnou na 2s nápis).

### Efekty

- **Double Strike** — `Shoting`: stisk spustí dávku `1 + stupeň` raket po `burstInterval` (0,1 s),
  časovač v `Update` (respektuje pauzu). Každá raketa `damage = 10 + bonus z kruhů`; bonus se
  vynuluje po celé dávce. Další dávka začne nejdřív po skončení předchozí a po `fireRate`.
- **Homing** — `DroneHUD`: velký **čtvercový zaměřovač** kolem středu, jen když hráč Homing má;
  velikost = základ + přírůstek za stupeň; zvýrazní se, když je uvnitř turret. `Shoting.Fire`:
  turret, jehož střed se promítne do čtverce (před kamerou, do dosahu), nejblíž středu → cíl
  rakety. `RocketProjectile`: s cílem zatáčí `turnRate` (základ + přírůstek za stupeň) stejně jako
  `HomingProjectile.Steer`, pak sweep jako dnes.
- **Lightning** — `RocketProjectile.HandleHit`: po zásahu turretu řetěz `stupeň` skoků; každý
  skok na nejbližší **ještě nezasažený** turret do `chainRange` (40 m) za `ceil(damage / 2)`,
  vizuál `ElectricZap` (modrý blesk). Pozice si vezme dřív, než zásah turret zničí.

Všechny konstanty (`burstInterval`, velikosti čtverce, `turnRate`, `chainRange`, podíl damage)
jsou pole v Inspectoru k ladění.

### Menu a obrazovky

- **Main menu → Upgrades:** katalog — všechny karty v mřížce, pod každou jméno a popis.
- **Obrazovka s výsledkem → Upgrades:** vybrané karty za run se stupněm (×2, ×3).
- ESC menu a konzole v sandboxu mají vlastní plány ([esc-menu.md](esc-menu.md),
  [sandbox-console.md](sandbox-console.md)) — použijí tytéž `GameSession` / `UpgradeCatalog`.

### Builder — `UpgradesBuilder`

`Tools > DroneMissile > Build Upgrades`: nastaví import obrázků v `Assets/UI/Upgrades/` (Sprite,
Point filtr, bez komprese) a vytvoří tři `UpgradeDefinition` assety — jen chybějící, ladění přežije.

### Soubory

- **Nové:** `UpgradeDefinition.cs`, `UpgradeRarity.cs`, `UpgradeCatalog.cs`,
  `Assets/Editor/UpgradesBuilder.cs`, assety v `Assets/Resources/Upgrades/`.
- **Úpravy:** `GameSession.cs`, `Shoting.cs`, `RocketProjectile.cs`, `DroneHUD.cs`, `RunUI.cs`,
  `MainMenu.cs`; dokumentace (rozhodnutí, featury, CLAUDE.md).

### Ověření

1. Headless kompilace + builder; kontrola assetů a importu obrázků.
2. Viktor: po vlně 1 se ukážou **3 různé karty** s obrázky; výběr kartou spustí další vlnu.
   Double Strike → dávka 2 raket, se dvěma 3; kruh + dávka → všechny rakety blikají a mají bonus.
   Homing → čtverec, raketa se stočí na turret ve čtverci; druhá karta → větší čtverec. Lightning →
   modrý skok na turret do 40 m, za druhou kartu další skok. Katalog v menu, seznam na konci runu,
   Hardcore bez nabídky, restart vlny na Easy karty ponechá.

### Mimo rozsah

Ukládání upgradů (Continue), ESC přehled, konzole v sandboxu, další upgrady.

## Otevřené otázky `[OTEVŘENÉ]`

1. **Další upgrady** — zatím tři (výš). Uncommon, Legendary a Mythic zatím nemají žádný —
   losování je **přeskočí** (rozhodnuto 04.10.2026).
2. **Barvy Legendary a Mythic** — potvrdit návrh.
3. ~~**Můžou se karty opakovat?**~~ **ROZHODNUTO 04.10.2026** — v jedné nabídce ne; v dalších
   vlnách ano, stupně se sčítají.
4. **Dá se výběr přeskočit**, nebo je volba povinná?
5. **Ovládání karet** — myš a šipky jako v menu; má jít vybírat i vysílačkou?
6. ~~**Po smrti na Easy**~~ — karty z předchozích vln zůstávají ([#19](../decisions.md)).

## Souvisí

[design/game-structure.md](../design/game-structure.md),
[features/between-wave-screen.md](../features/between-wave-screen.md),
[features/end-screen.md](../features/end-screen.md), [features/main-menu.md](../features/main-menu.md).
