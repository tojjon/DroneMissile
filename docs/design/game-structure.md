# Struktura hry — aréna, vlny, karty, menu

Předělání hry z jedné scény s jedním turretem na **arénový survival po vlnách s výběrem upgradů**
(roguelite smyčka). Zdroj: Viktorovy podklady *Základní info o hře* a *Wavky*, 03.10.2026.
Rozhodnutí o směru je [#18](../decisions.md).

Nic z tohohle zatím není naprogramované. Obsah vln a bosse je v [waves.md](waves.md), typy turretů
v [enemies.md](enemies.md).

## Herní smyčka `[ZADÁNÍ]`

```
Main menu ──► New game / Continue
                 │
                 ▼
          ┌─► vlna N ── všichni nepřátelé zničeni ──► pauza: výběr z 5 karet ──┐
          │                                                                  │
          └──────────────────────── N + 1 ◄──────────────────────────────────┘
                 (každá 10. vlna je boss)
```

1. **Hraje se ve velkém uzavřeném boxu** — aréna se stěnami a stropem, ne otevřený terén.
2. **Hra je rozdělená na vlny.** Každá **desátá** vlna je **boss** (10, 20, 30, …).
3. **Po každé vlně se hra zastaví** a nabídne **5 náhodně vybraných karet** (upgradů). Hráč si
   jednu vybere a začne další vlna.

## Karty a rarity `[ZADÁNÍ]`

Každá z 5 nabízených karet má rarity losovanou podle těchto vah:

| Rarita | Barva | Šance |
|---|---|---|
| Common | šedá | 60 % |
| Uncommon | zelená | 20 % |
| Rare | modrá | 10 % |
| Epic | fialová | 6 % |
| Legendary | *neurčeno* | 3 % |
| Mythic | *neurčeno* | 1 % |

Váhy dávají dohromady 100 %. Karty jsou upgrady dronu — **konkrétní seznam upgradů zatím
neexistuje**, viz otevřené otázky.

## Main menu `[ZADÁNÍ]`

Hra po spuštění naběhne do hlavního menu:

| Položka | Co dělá |
|---|---|
| **Continue** | Pokračuje v rozehrané hře. Jen pokud nějaká existuje. |
| **New game** | Založí novou hru od vlny 1. |
| **Sandbox** | Volný svět na zkoušení upgradů — viz níž. |
| **Upgrades** | Katalog všech upgradů: obrázek a popis, co dělá. |

### Sandbox

Otevřený svět bez vln. Po stisku **Enter** se otevře pole, hráč napíše **jméno upgradu** a dron
ho dostane. Smysl: vyzkoušet si jednotlivé upgrady a jejich kombinace bez nutnosti je vyhrát v runu.

## Pauza (ESC) `[ZADÁNÍ]`

**ESC** hru zastaví a ukáže menu:

| Položka | Co dělá |
|---|---|
| **Resume** | Pokračuje ve hře. |
| **Upgrades** | Ukáže upgrady, které dron právě má. |
| **Main menu** | Návrat do hlavního menu. |
| **Quit** | Ukončí hru. |

## Co z toho plyne pro implementaci

Ne zadání, ale důsledky, které je dobré mít na očích, než se do toho pustíme:

- **Víc scén.** Main menu, aréna a sandbox — dnes je v buildu jen `SampleScene`.
- **Ukládání.** *Continue* potřebuje uložit aspoň číslo vlny a vlastněné upgrady.
- **Systém upgradů.** Karty, katalog v menu, ESC přehled i sandbox čtou **jeden** seznam upgradů
  podle jména — upgrade musí jít dronu přidat za běhu.
- **Pauza.** Výběr karet i ESC menu zastavují hru (`Time.timeScale`), a dron s Rigidbody musí
  pauzu přežít beze změny.
- **Kolize s [rozhodnutím #10](../decisions.md).** Dnes jakýkoli dotek pevného objektu restartuje
  scénu. V uzavřeném boxu to znamená, že **stěny a strop zabíjí**, a restart scény by smazal celý
  run i vybrané karty — viz otázka 1.

## Otevřené otázky `[OTEVŘENÉ]`

1. **Co je smrt v runu?** Dron nemá HP a pád dnes znovu načte scénu ([#10](../decisions.md)). Má
   smrt ukončit celý run (zpět do menu), nebo restartovat jen aktuální vlnu? Zabíjí i stěny boxu?
2. **Jaké upgrady existují?** Bez seznamu nejde udělat karty, katalog ani sandbox. Kolik jich je
   v každé raritě?
3. **Barvy Legendary a Mythic.** Návrh: Legendary oranžová / zlatá, Mythic červená — potvrdit.
4. **Můžou se karty opakovat?** Ve stejné nabídce, a znovu ten samý upgrade v dalších vlnách
   (stackování)?
5. **Jak velký je box** a co je uvnitř — rovná podlaha, překážky, kryty?
6. **Kdy se Continue ukládá?** Po každé vlně (po výběru karty), nebo kdykoli?
7. **Sandbox: jaký svět?** Dnešní terén z `SampleScene`, nebo nová mapa? Jsou v něm nepřátelé
   na zkoušení? A jak se upgrade odebírá?
8. **Ovládání menu vysílačkou.** Menu jde přirozeně myší/klávesnicí — má jít projet i vysílačkou?
   Sandbox (psaní jména) bez klávesnice nepůjde.
9. **Končí hra někdy?** Vyhrává se po určitém bossovi, nebo jsou vlny nekonečné?
