# Rakety hráče

**Stav:** hotovo · otestováno hraním: ano · `[OVĚŘENO 04.10.2026]`

## Co to je

Jediná zbraň dronu: **nevedená raketa** vystřelená ze špičky dronu. Letí rovně ve směru, kam dron
mířil; dá se jí trefit jen přesným letem. Žádný hitscan ([rozhodnutí #3](../decisions.md)).

## Jak se to chová

- Zaměřovací křížek uprostřed obrazovky je **přesný bod dopadu** na jakoukoli vzdálenost.
- Zásah se detekuje **sweep testem** (raycast úseku, který raketa za krok urazí) — nic neprolétne,
  ani při 120 m/s ([rozhodnutí #17](../decisions.md)).
- Každý dopad udělá červený výbuch, viz [impact-effects.md](impact-effects.md).
- Turret má 100 HP, raketa dává 10 → **10 zásahů** na turret.
- Bonus ze **žlutých kruhů** se přičte k příští raketě a ta bliká žlutě
  ([yellow-rings.md](yellow-rings.md)).
- Na pauze (obrazovky mezi vlnami a na konci) se nestřílí.

## Ladění

| Parametr | Hodnota | Kde |
|---|---|---|
| `fireRate` | 0,5 s mezi výstřely | `Shoting` na dronu |
| `speed` | 120 m/s | `rocket.prefab` |
| `lifeTime` | 5 s | `rocket.prefab` |
| `damage` | 10 | `RocketProjectile` (od #23 pole, ne zadrátovaná hodnota) |

## Kde v projektu

`Assets/scripts/Shoting.cs`, `Assets/scripts/RocketProjectile.cs`, `Assets/3D models/rocket.prefab`.

## Souvisí

[design/weapons.md](../design/weapons.md), rozhodnutí [#3](../decisions.md),
[#17](../decisions.md), [#23](../decisions.md).
