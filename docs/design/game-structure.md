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
| **New game** | Hráč zvolí **obtížnost** (viz níž) a začne novou hru od vlny 1. |
| **Sandbox** | Volný svět na zkoušení upgradů — viz níž. |
| **Upgrades** | Katalog všech upgradů: obrázek a popis, co dělá. |

### Sandbox

Otevřený svět bez vln. Po stisku **Enter** se otevře pole, hráč napíše **jméno upgradu** a dron
ho dostane. Smysl: vyzkoušet si jednotlivé upgrady a jejich kombinace bez nutnosti je vyhrát v runu.

## Obtížnost a smrt `[ZADÁNÍ]`

Obtížnost se volí **při zakládání nové hry** a určuje, co se stane, když dron zemře
([rozhodnutí #19](../decisions.md)):

| Obtížnost | Smrt |
|---|---|
| **Easy** | **Znovu se načte aktuální vlna.** Run pokračuje — vlna začne od začátku, karty z předchozích vln zůstávají. |
| **Normal** | **Run končí** → obrazovka s výsledkem (viz níž). |

Co je smrt, se nemění: dron nemá HP a umírá dotekem pevného objektu ([#10](../decisions.md)).

### Stěny a strop boxu

Náraz do **stěny nebo stropu** arény se liší podle obtížnosti ([rozhodnutí #20](../decisions.md)):

| Obtížnost | Náraz do stěny / stropu |
|---|---|
| **Easy** | **omráčí** — stejný stun jako nepřátelská střela, dron spadne, pokud ho hráč nevybere |
| **Normal** | **zabije** — jako jakýkoli jiný dotek pevného objektu |

### Obrazovka s výsledkem (konec runu)

Po konci runu na Normal se ukáže souhrn, z něj se jde do main menu. Statistiky:

- **dosažená vlna** v tomhle runu,
- **nejlepší vlna** — rekord přes všechny runy,
- **udělený damage** — v posledním kole i za celý run,
- a další podobné (Viktor: „atd.") — viz otevřené otázky.

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
- **Smrt už nesmí načítat celou scénu.** Dnes ji [rozhodnutí #10](../decisions.md) restartuje
  přes `SceneManager.LoadScene`, což by smazalo run i karty. Easy potřebuje vrátit **jen vlnu**
  (nepřátele, pozici dronu) a stav runu držet mimo scénu.
- **Obtížnost patří do uložené hry**, aby ji *Continue* znal.

## Otevřené otázky `[OTEVŘENÉ]`

1. ~~**Co je smrt v runu?**~~ **ROZHODNUTO 03.10.2026** — podle obtížnosti, viz
   sekci *Obtížnost a smrt* výš a [rozhodnutí #19](../decisions.md). Stěny a strop
   řeší [rozhodnutí #20](../decisions.md), konec runu na Normal vede přes obrazovku s výsledkem.
2. ~~**Jaké upgrady existují?**~~ **ODLOŽENO 03.10.2026** — zatím žádné, Viktor je bude přidávat
   postupně. Systém karet, katalog i sandbox se tedy staví na **prázdný, rozšiřitelný seznam**:
   přidání upgradu má znamenat jeden nový záznam, ne změny v menu.
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
10. **Které další statistiky na obrazovku s výsledkem?** Návrhy: počet zničených turretů, přesnost
    (zásahy / výstřely), čas runu, počet průletů žlutými kruhy, vybrané karty. „Nejlepší vlna" se
    musí ukládat mimo run, aby přežila konec hry — rekordy jen pro Normal, nebo zvlášť pro každou
    obtížnost?
