# Dokumentace — FPV Drone Sim → Shooter

Tady žije **záměr**: co hra má být, proč jsme se rozhodli tak, jak jsme se rozhodli, a co je
hardwarová realita, kterou nejde odvodit z kódu. Implementaci popisuje `../CLAUDE.md` a samotný kód.

Dělicí linka, ať se to nerozjede:

| Kde | Co tam patří | Co tam nepatří |
|---|---|---|
| `docs/` | záměr, herní design, rozhodnutí + jejich důvody, hardwarová fakta | jak je to naprogramované |
| `CLAUDE.md` | jak je to naprogramované, příkazy, pasti v repu | proč jsme to tak chtěli |
| kód | pravda o implementaci | pravda o záměru |

Když se rozejde `docs/` a kód, **`docs/` vyhrává** — to znamená, že kód je pozadu, ne že dokumentace
je špatná. Jedinou výjimkou jsou bloky označené `[OVĚŘENO]`, které záměrně popisují realitu.

## Mapa

| Soubor | K čemu |
|---|---|
| [concept.md](concept.md) | vize, herní pilíře, rozsah, co ještě není rozhodnuté |
| [design/flight-model.md](design/flight-model.md) | letová fyzika — acro požadavky a tuning |
| [design/controls.md](design/controls.md) | mapování os (Mode 2), klávesnice, filozofie ovládání |
| [design/weapons.md](design/weapons.md) | zbraně hráče, pravidlo „fyzické projektily" |
| [design/enemies.md](design/enemies.md) | turrety, jejich barevné typy a budoucí nepřátelé |
| [design/game-structure.md](design/game-structure.md) | herní smyčka — aréna, vlny, karty upgradů, main menu, ESC, sandbox |
| [design/waves.md](design/waves.md) | obsah vln, boss, žluté kruhy |
| [reference/radiomaster-pocket.md](reference/radiomaster-pocket.md) | HID detaily vysílačky — draze zjištěné, needitovat z hlavy |
| [reference/unity-gotchas.md](reference/unity-gotchas.md) | Unity pasti, na které jsme narazili |
| [decisions.md](decisions.md) | rozhodovací log — jedno rozhodnutí = jeden záznam, append-only |
| [backlog.md](backlog.md) | otevřené bugy a nápady, s ověřeným stavem |

## Značky

Používané v celé dokumentaci, aby bylo poznat, čemu se dá věřit:

- **`[ZADÁNÍ]`** — Viktorův požadavek na design. Není to popis kódu, je to cíl.
- **`[OVĚŘENO dd.mm.yyyy]`** — zkontrolováno proti souborům projektu k danému datu. Časem zvětrá.
- **`[HYPOTÉZA]`** — domněnka, kterou nikdo neověřil. Nezacházet s tím jako s faktem.
- **`[OTEVŘENÉ]`** — čeká na společné rozhodnutí. Tohle jsou háčky pro další session.

## Jak s tím pracovat

Když se pustíme do nějakého systému („pojďme řešit turrety"), otevři jeho soubor v `design/` —
sekce `[OTEVŘENÉ]` na konci je seznam toho, co je potřeba dořešit.

Když padne rozhodnutí, zapiš ho do [decisions.md](decisions.md) **i** do příslušného `design/`
souboru: log drží *proč* a kdy, design drží *aktuální platný stav*. Bez toho se za měsíc znovu
hádáme o věci, která už byla rozhodnutá.

Rozhodnutí se nepřepisují. Když se něco změní, přidá se nový záznam, který ten starý nahradí.
