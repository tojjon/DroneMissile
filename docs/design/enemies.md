# Nepřátelé

Zatím existuje jediný typ: **pozemní turret**.

## Turret — herní požadavky `[ZADÁNÍ]`

- **Vzhled:** pozemní věž (základna + otočná hlaveň), **ne humanoidní nepřítel**.
- **Detekce:** jednoduchý dosah (`detectionRange`). **Žádné line-of-sight ani zorné pole** — turret
  vidí skrz terén i překážky. Zjednodušení, které je zatím v pořádku.
- **Střelba:** vlastní fyzický projektil, ne hitscan → hráč má šanci uhnout.
- **HP:** turret přežije víc zásahů. **Žádný one-shot kill.**
- **Míření:** hlaveň se natáčí za hráčem **horizontálně i vertikálně**. První verze měla záměrně jen
  horizontální otáčení (`direction.y = 0`), pak rozšířeno na plné 3D míření.
- **Přesnost omezuje `turnSpeed`, ne rozptyl.** Turret střílí podél hlavně, takže dokud se hlaveň
  dotáčí, míjí — to je záměr a hráč to má využívat. Viz [rozhodnutí #9](../decisions.md).

## Parametry `[OVĚŘENO 03.09.2026]`

| Parametr | Hodnota |
|---|---|
| `detectionRange` | 30 |
| `fireRate` | 2 s |
| `turnSpeed` | 90 °/s |
| `maxHealth` | 30 |
| projektil `speed` / `lifeTime` / `damage` | 20 / 5 s / 10 |

Turret je zatím jediný ve scéně a je jediná věc ve hře, která **umí zemřít** (`Destroy(gameObject)`).

## Struktura objektu `[OVĚŘENO 04.09.2026]`

Hierarchie **není** kosmetická — vynucují ji dvě věci: Unity bug s nerovnoměrným scale
([rozhodnutí #4](../decisions.md)) a konvence +Z = forward ([rozhodnutí #9](../decisions.md)). Viz
[reference/unity-gotchas.md](../reference/unity-gotchas.md).

```
turret                    ← prázdný parent, skript EnemyTurret sedí ZDE, tag Untagged
├── Turret_Base           ← sourozenec, tag Enemy, scale (5, 1, 5)
└── Turret_Barrel         ← sourozenec (NE child Base!), tag Enemy, scale (2,2,2), tímhle se míří
    ├── Turret_BarrelMesh ← jen vizuál, rotace 90° X (kapsle má dlouhou osu Y)
    └── TurretFirePoint   ← ústí, MUSÍ ležet na ose +Z hlavně
```

`Turret_Barrel` je **aim transform** — `LookRotation` mu zarovnává +Z na hráče. Mesh kapsle tu
konvenci nesplňuje, proto je odstrčený do childa `Turret_BarrelMesh`; na aim transform nepatří.

Hitboxy: `BoxCollider` na `turret` rootu pokrývá základnu, `CapsuleCollider` na `Turret_Barrel`
(direction Z) hlaveň a otáčí se s ní. Root **nemá** `CapsuleCollider` — ten původní byl
nadimenzovaný na svisle stojící kapsli a po srovnání hlavně by z něj zůstal 4,5 m vysoký neviditelný
sloup nad základnou, do kterého by se registrovaly zásahy do prázdna.

Tag `Enemy` je na `Turret_Base` **i** `Turret_Barrel`, ale od [rozhodnutí #8](../decisions.md) ho
nečte žádný kód.

**Korekce:** `Turret_Base` má scale **`(5, 1, 5)`** `[OVĚŘENO 04.09.2026]`. Rozhodnutí #4 uvádí
`(1.5, 0.3, 1.5)` — zvětralá hodnota, ale log je append-only, takže se tam nepřepisuje. Na podstatě
#4 to nic nemění, nerovnoměrný je scale v obou případech.

## Otevřené otázky `[OTEVŘENÉ]`

1. **Víc typů nepřátel?** Raketová baterie s vedenými střelami, mobilní jednotka, letecký nepřítel
   (což by teprve byl souboj pro FPV pilota)?
2. **Line-of-sight.** Teď turret střílí skrz kopce. Přidat raycast? Změnilo by to gameplay — terén
   by se stal krytem, což je pro FPV let hodně zajímavé.
3. **Reakční doba a předvídání.** Turret míří přesně na aktuální pozici hráče. Nemá střílet
   s předstihem (lead), aby to bylo férovější i nebezpečnější?
4. **Chování po zásahu.** Teď zmizí bez efektu. Výbuch, trosky, oheň?
5. **Kolik turretů a jak rozmístěné?** Souvisí s otázkou úrovní v [concept.md](../concept.md).
6. **Má turret přestat střílet, když hráč zmizí z dosahu?** Teď při ztrátě dosahu jen přestane
   `Update` — ale cooldown běží dál, takže první výstřel po návratu do dosahu přijde okamžitě.
