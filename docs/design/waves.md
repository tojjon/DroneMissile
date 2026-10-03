# Vlny a boss

Obsah jednotlivých vln. Smyčka kolem nich (pauza, karty, menu) je v
[game-structure.md](game-structure.md), typy turretů v [enemies.md](enemies.md). Zdroj: Viktorův
podklad *Wavky*, 03.10.2026.

## Vlny 1–10 `[ZADÁNÍ]`

| Vlna | Obsah |
|---|---|
| 1 | **jeden** šedý turret |
| 2 | šedé turrety (víc než jeden, počet neurčen) |
| 3–9 | *zatím nenavrženo* |
| 10 | **Boss** |

## Boss (vlna 10) `[ZADÁNÍ]`

**Velký šedý turret.**

- **Health bar nahoře na obrazovce** — v HUDu, ne ve světě. Je to výjimka z
  [rozhodnutí #13](../decisions.md), které pro běžné turrety drží pruh ve world space nad turretem.
- **Silné střely.**
- **Od poloviny HP** začne střílet **nahoru, v různých směrech, barevné kuličky** v barvách typů
  turretů. Kde kulička dopadne, tam se **spawne turret té barvy**.
- **Počet kuliček je proměnná** nastavitelná v Inspectoru.

## Žluté kruhy `[ZADÁNÍ]`

V **každé vlně** se spawnují **žluté kruhy**. Když jimi dron proletí:

- **příští střela** dá **+1 damage**,
- a ta střela **bliká žlutě**, aby bylo vidět, že je posílená.

Sedí to na pilíř „letová fyzika je základ" ([concept.md](../concept.md)) — bonus se získává
přesným průletem, ne sbíráním.

## Otevřené otázky `[OTEVŘENÉ]`

1. **Vlny 3–9.** Návrh k potvrzení: postupně přidávat nové barvy turretů (modrý, červený, zelený)
   a zvyšovat jejich počet, aby boss na vlně 10 kombinoval všechno, co hráč už potkal.
2. **Co po vlně 10?** Opakuje se cyklus těžší (vlny 11–19 + boss na 20), nebo hra končí?
3. **Kde se turrety spawnují?** Náhodně na podlaze boxu, nebo na pevně daných místech?
4. **Boss — „silné střely".** Jaký projektil? Vlastní typ, nebo šedý (stun) s vyšším účinkem?
   Dron nemá HP ([#10](../decisions.md)), takže „silnější" musí znamenat něco jiného než damage —
   delší stun, větší projektil, rychlejší palba?
5. **Boss — kuličky.** Střílí je jednou při dosažení poloviny HP, nebo opakovaně až do smrti?
   Barvy náhodně, nebo ve stejném poměru? Musí spawnuté turrety hráč zničit, aby vlna skončila?
6. **Boss — kolik má HP** a dá se poškodit, když hráč neničí přivolané turrety?
7. **Žluté kruhy — kolik a kde?** Kolik jich je ve vlně, kde se objevují, zmizí po průletu?
8. **Žluté kruhy — stackují se?** Dva průlety = +2 na příští střelu, nebo +1 na dvě střely, nebo
   jen +1 bez ohledu na počet? A **+1 k čemu**: raketa dnes dává 10, turret má 100 HP
   ([#14](../decisions.md)), takže +1 je bonus 10 % — je to záměr, nebo se má škálovat?
