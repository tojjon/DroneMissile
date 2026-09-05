# Zbraně hráče

## Pravidlo: fyzické projektily `[ZADÁNÍ]`

**Žádný hitscan — na obou stranách.** Všechno, co se vystřelí, je objekt letící prostorem, kterému
se dá uhnout. Platí to i pro nepřátele, viz [enemies.md](enemies.md) a
[rozhodnutí #3](../decisions.md).

Důsledek pro design: zásah je vždy výsledek pozice a letu, nikdy hodu kostkou. Proto v projektu není
a nemá být žádný raycast pro střelbu, přesnost v procentech ani rozptyl.

## Raketa hráče `[OVĚŘENO 05.09.2026]`

| Parametr | Hodnota | Kde |
|---|---|---|
| `fireRate` | 0,5 s mezi výstřely | `Shoting` |
| `speed` | 30 (**strop ~49**, viz níž) | `rocket.prefab` |
| `lifeTime` | 5 s | `rocket.prefab` |
| damage turretu | **10, hardcoded** | `RocketProjectile.OnTriggerEnter` |

Raketa je **nevedená** — letí přímo ve směru, ve kterém byla vypuštěna (`transform.forward` z
`FirePoint`). Turret má po [rozhodnutí #14](../decisions.md) **100 HP** a raketa dává 10 →
**10 zásahů na zničení**.

Poznámka k asymetrii: damage rakety hráče je zadrátovaný v místě volání. Protějšek
`EnemyProjectile.damage` neexistuje — dron nemá HP a zásah místo poškození bere ovládání
([rozhodnutí #10](../decisions.md)), takže je to jediná zadrátovaná hodnota poškození ve hře.

## Strop rychlosti projektilu

`speed` nesmí přesáhnout **~49 m/s**. Není to designové rozhodnutí, je to technický limit detekce
zásahu: ta je diskrétní a spolehlivá jen dokud je posun za fyzikální krok (`speed × 0,02` s) menší
než délka collideru projektilu (0,986 m). Nad tím projektily začnou prolétávat cíli.

**Platí to pro raketu hráče, ne pro nepřátelskou.** `rocket.prefab` má `speed: 30`, hluboko pod
stropem — na spolehlivé detekci tady stojí jediný způsob, jak turret zabít. Nepřátelský
`enemy_rocket.prefab` má od [rozhodnutí #14](../decisions.md) `speed: 1080` a strop tím **vědomě
porušuje**: 21,6 m za krok znamená, že dron trefí zhruba v 6 % případů. Hraje se to tak dobře, ale
je to náhodná vzácnost, ne navržená. Kdyby byla potřeba raketa rychlá **i spolehlivá**, je nutné
znovu otevřít [rozhodnutí #8](../decisions.md) (sweep test po dráze mezi dvěma kroky), ne jen
šroubovat číslem.

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
