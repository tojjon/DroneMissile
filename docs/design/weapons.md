# Zbraně hráče

## Pravidlo: fyzické projektily `[ZADÁNÍ]`

**Žádný hitscan — na obou stranách.** Všechno, co se vystřelí, je objekt letící prostorem, kterému
se dá uhnout. Platí to i pro nepřátele, viz [enemies.md](enemies.md) a
[rozhodnutí #3](../decisions.md).

Důsledek pro design: zásah je vždy výsledek pozice a letu, nikdy hodu kostkou. Proto v projektu není
a nemá být hitscan, přesnost v procentech ani rozptyl.

**Upřesnění k raycastu `[AKTUALIZOVÁNO 05.09.2026]`:** zákaz míří na **hitscan** — na střelu, která
zasáhne okamžitě a na libovolnou vzdálenost. Neplatí na raycast použitý jako **detekce kolize už
letícího projektilu**. Sweep test v `RocketProjectile` ([rozhodnutí #17](../decisions.md)) testuje
jen ten úsek, který raketa urazí za jeden fyzikální krok (2,4 m) — raketa dál fyzicky letí, dá se jí
uhnout a trvá jí, než doletí. Je to spojitá varianta téhož testu, který dřív dělal trigger, ne
zkratka k cíli.

## Raketa hráče `[OVĚŘENO 05.09.2026]`

| Parametr | Hodnota | Kde |
|---|---|---|
| `fireRate` | 0,5 s mezi výstřely | `Shoting` |
| `speed` | **120** (strop ~49 už neplatí, viz níž) | `rocket.prefab` |
| `lifeTime` | 5 s | `rocket.prefab` |
| damage turretu | **10, hardcoded** | `RocketProjectile.HandleHit` |
| detekce zásahu | **raycast sweep** v `FixedUpdate` | `RocketProjectile.Sweep` |

Raketa je **nevedená** — letí přímo ve směru, ve kterém byla vypuštěna (`transform.forward` z
`FirePoint`). Turret má po [rozhodnutí #14](../decisions.md) **100 HP** a raketa dává 10 →
**10 zásahů na zničení**.

Poznámka k asymetrii: damage rakety hráče je zadrátovaný v místě volání. Protějšek
`EnemyProjectile.damage` neexistuje — dron nemá HP a zásah místo poškození bere ovládání
([rozhodnutí #10](../decisions.md)), takže je to jediná zadrátovaná hodnota poškození ve hře.

## Efekt zásahu `[AKTUALIZOVÁNO 05.09.2026]`

Raketa při zániku vyrobí krátký červený výbuch — záblesk, kužel jisker odražený od povrchu a krátké
bodové světlo. Spouští se při **každém** zásahu, ne jen o zem: terén, deska i turret.

| Parametr | Hodnota | Kde |
|---|---|---|
| `impactFx` | zapnuto | `RocketProjectile` |
| `impactColor` | HDR `(8, 1,0, 0,25)` | `RocketProjectile` |
| `impactScale` | 1 | `RocketProjectile` |
| `impactBackOffset` | 0,15 m po normále povrchu | `RocketProjectile` |
| délka | ~0,45 s | `ImpactExplosion` |

Efekt se staví v kódu (`ImpactExplosion`, materiály z `FxAssets`) — žádný prefab, viz
[rozhodnutí #16](../decisions.md).

**Barva je HDR a musí jít přes materiál, ne přes částici.** Bloom má threshold 1, ale
`main.startColor` jde do vertex streamu jako `Color32` a cokoli nad 1 by se oříznulo — efekt by
nezářil. Tint se proto posílá přes `MaterialPropertyBlock` na `_BaseColor`
(`FxAssets.Tint`), viz [rozhodnutí #17](../decisions.md) a
[reference/unity-gotchas.md](../reference/unity-gotchas.md).

**Pozice i orientace jsou přesné**, protože je od [rozhodnutí #17](../decisions.md) dodá sweep test:
`hit.point` a `hit.normal`. Kužel jisker tak odletí od skutečného povrchu, ne po odhadu ze směru
letu. `other.ClosestPoint()` se tu použít **nesmí** — je definovaný jen pro Box/Sphere/Capsule a
**konvexní** mesh collidery, a to, do čeho raketa trefuje nejčastěji, je `TerrainCollider` a
nekonvexní meshe, kde vrátí počátek transformu collideru, tedy výbuch o stovky metrů vedle.

## Strop rychlosti projektilu `[AKTUALIZOVÁNO 05.09.2026]`

Strop **~49 m/s** platil, dokud detekce stála na diskrétním triggeru: ta je spolehlivá jen dokud je
posun za fyzikální krok (`speed × 0,02` s) menší než délka collideru projektilu (0,986 m). Nad tím
projektily začnou prolétávat cíli.

**Raketa hráče strop nepotřebuje — od [rozhodnutí #17](../decisions.md) používá sweep test.**
`rocket.prefab` má `speed: 120`, tedy 2,4 m za krok proti collideru 0,986 m; na triggeru by
**prolétla zhruba 59 % zásahů**. (Dokumentace tady dlouho tvrdila `speed: 30`; byla zastaralá,
prefab je autoritativní.) `FixedUpdate` proto raycastuje úsek, který raketa v tom kroku urazí, a
detonuje v `hit.point`. Detekce je tím přesná při jakékoli rychlosti a jako bonus vrací **skutečnou
normálu povrchu** pro efekt zásahu. `OnTriggerEnter` zůstává jen jako záloha za `consumed` guardem.

**Nepřátelská raketa strop dál porušuje, a to schválně.** `enemy_rocket.prefab` má od
[rozhodnutí #14](../decisions.md) `speed: 1080`: 21,6 m za krok znamená, že dron trefí zhruba v 6 %
případů. Hraje se to tak dobře, ale je to *náhodná* vzácnost, ne navržená. Kdyby byla potřeba i
**spolehlivá**, dostane stejný sweep jako raketa hráče — ale změní to ladění boje.

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
