# Upgrady a karty

**Stav:** plánováno — místa v UI jsou připravená, **upgrady samotné zatím neexistují.** Viktor je
bude vymýšlet postupně.

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

## Návrh implementace

Ne zadání — návrh, jak to postavit, aby **přidání upgradu znamenalo jeden nový záznam, ne změny
v menu** (požadavek z [design/game-structure.md](../design/game-structure.md)):

- **Definice upgradu jako asset** (`ScriptableObject`): jméno, popis, obrázek, rarita. Jeden soubor
  na upgrade, všechny v jedné složce. Katalog, karty i sandbox je najdou samy.
- **Efekt** upgradu: komponenta, kterou upgrade přidá dronu, nebo úprava hodnot (rychlost palby,
  damage, tah …). Podle toho, jaké upgrady Viktor vymyslí.
- **Vlastněné upgrady** si pamatuje `GameSession` (přežijí načtení scény; patří i do uloženého
  runu, viz [save-continue.md](save-continue.md)).
- **Losování:** nejdřív rarita podle vah, pak náhodný upgrade té rarity. Když rarita nemá žádný
  upgrade, losuje se znovu.
- **Barva karty** podle rarity.

## Otevřené otázky `[OTEVŘENÉ]`

1. **Jaké upgrady existují?** Kolik v každé raritě? (Viktor dodá postupně.)
2. **Barvy Legendary a Mythic** — potvrdit návrh.
3. **Můžou se karty opakovat?** Dvakrát tentýž v jedné nabídce? Tentýž upgrade znovu v další vlně
   (stackování)?
4. **Dá se výběr přeskočit**, nebo je volba povinná?
5. **Ovládání karet** — myš a šipky jako v menu; má jít vybírat i vysílačkou?
6. **Po smrti na Easy** — zůstávají karty z předchozích vln? Podle [#19](../decisions.md) ano.

## Souvisí

[design/game-structure.md](../design/game-structure.md),
[features/between-wave-screen.md](../features/between-wave-screen.md),
[features/end-screen.md](../features/end-screen.md), [features/main-menu.md](../features/main-menu.md).
