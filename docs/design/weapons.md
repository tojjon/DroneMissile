# Zbraně hráče

## Pravidlo: fyzické projektily `[ZADÁNÍ]`

**Žádný hitscan — na obou stranách.** Všechno, co se vystřelí, je objekt letící prostorem, kterému
se dá uhnout. Platí to i pro nepřátele, viz [enemies.md](enemies.md) a
[rozhodnutí #3](../decisions.md).

Důsledek pro design: zásah je vždy výsledek pozice a letu, nikdy hodu kostkou. Proto v projektu není
a nemá být žádný raycast pro střelbu, přesnost v procentech ani rozptyl.

## Raketa hráče `[OVĚŘENO 03.09.2026]`

| Parametr | Hodnota | Kde |
|---|---|---|
| `fireRate` | 0,5 s mezi výstřely | `Shoting` |
| `speed` | 30 | `rocket.prefab` |
| `lifeTime` | 5 s | `rocket.prefab` |
| damage turretu | **10, hardcoded** | `RocketProjectile.OnCollisionEnter` |

Raketa je **nevedená** — letí přímo ve směru, ve kterém byla vypuštěna (`transform.forward` z
`FirePoint`). Turret má 30 HP a raketa dává 10 → **3 zásahy na zničení**.

Poznámka k asymetrii: damage rakety hráče je zadrátovaný v místě volání, kdežto nepřátelský projektil
má `damage` jako pole v Inspectoru. Není to záměr, jen to tak vzniklo.

## Známý problém

Rakety občas prolétnou turretem bez kolize. Root cause je diagnostikovaný a je fyzikální, ne
designový — viz [backlog.md](../backlog.md).

## Otevřené otázky `[OTEVŘENÉ]`

1. **Jen jedna zbraň?** Teď existují jen nevedené rakety. Přidat druhou zbraň (kulomet, vedená
   raketa, bomba)? Pokud ano, jak se přepíná — vysílačka nemá moc volných tlačítek.
2. **Munice a přebíjení?** Teď nekonečné rakety s cooldownem.
3. **Má raketa výbuch s plošným poškozením?** Teď zasáhne přesně jeden objekt a zmizí.
4. **Vedená raketa (lock-on)?** Tematicky sedí k „raketometu", ale kolidovalo by to s pilířem
   „přesnost je zásluha hráče".
5. **Zpětný ráz?** Výstřel by mohl dron odhodit — pro acro pilota zajímavá mechanika.
6. **Vizuál a zvuk.** Materiály `Laser.mat` a `enemy laser.mat` v projektu existují, ale rakety mají
   být rakety, ne lasery — je to zbytek staršího nápadu? `[OTEVŘENÉ]`
