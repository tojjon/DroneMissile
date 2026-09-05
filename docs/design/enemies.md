# Nepřátelé

Zatím existuje jediný typ: **pozemní turret**.

## Turret — herní požadavky `[ZADÁNÍ]`

- **Vzhled:** pozemní věž (základna + otočná hlaveň), **ne humanoidní nepřítel**.
- **Detekce:** jednoduchý dosah (`detectionRange`). **Žádné line-of-sight ani zorné pole** — turret
  vidí skrz terén i překážky. Zjednodušení, které je zatím v pořádku.
- **Střelba:** vlastní fyzický projektil, ne hitscan → hráč má šanci uhnout.
- **HP:** turret přežije víc zásahů. **Žádný one-shot kill.** Zbývající HP jsou vidět jako **pruh
  nad turretem**, viz [rozhodnutí #13](../decisions.md).
- **Míření:** hlaveň se natáčí za hráčem **horizontálně i vertikálně**. První verze měla záměrně jen
  horizontální otáčení (`direction.y = 0`), pak rozšířeno na plné 3D míření.
- **Přesnost omezuje `turnSpeed`, ne rozptyl.** Turret střílí podél hlavně, takže dokud se hlaveň
  dotáčí, míjí — to je záměr a hráč to má využívat. Viz [rozhodnutí #9](../decisions.md).

## Parametry `[OVĚŘENO 05.09.2026]`

Platí hodnoty **ze scény a z prefabu**, ne defaulty ve skriptech — přeladěno podle hraní, viz
[rozhodnutí #14](../decisions.md).

| Parametr | Hodnota ve scéně | Default v `EnemyTurret.cs` |
|---|---|---|
| `detectionRange` | **360** | 30 |
| `fireRate` | **0,2 s** (5 ran/s) | 2 s |
| `turnSpeed` | **720 °/s** | 90 °/s |
| `maxHealth` | **100** → 10 zásahů | 30 |

| Nepřátelský projektil (`enemy_rocket.prefab`) | Hodnota | Default v `EnemyProjectile.cs` |
|---|---|---|
| `speed` | **1080** (nad stropem, viz níž) | 20 |
| `lifeTime` | 5 s | 5 s |
| `stunDuration` | 1 s | 1 s |

Zásah nedává poškození — dron **nemá HP** a místo toho na `stunDuration` přijde o ovládání, viz
[rozhodnutí #10](../decisions.md). I-frames na straně dronu (`stunImmunity`) jsou ve scéně **0,5 s**.

**Nepřátelská raketa při `speed: 1080` většinou proletí skrz.** Detekce zásahu je diskrétní a
21,6 m za fyzikální krok je mnohem víc než collider (0,986 m), takže dron schytá zhruba **6 %**
přímých zásahů. Je to vědomá cena za pocit ze hry, ne bug — celý rozbor je v
[rozhodnutí #14](../decisions.md).

Turret je zatím jediný ve scéně a je jediná věc ve hře, která **umí zemřít** (`Destroy(gameObject)`).

## Zobrazení HP `[OVĚŘENO 05.09.2026]`

Pruh života visí **ve světě nad turretem**, ne v HUDu — patří konkrétnímu turretu, takže ho zakrývá
terén a zmenšuje se s dálkou. Detaily a co to vylučuje jsou v [rozhodnutí #13](../decisions.md).

| Parametr (`TurretHealthBar`) | Hodnota ve scéně | Default ve skriptu |
|---|---|---|
| `heightOffset` | **4,8 m** nad rootem | 3,2 m |
| `width` / `height` | **2,7 × 0,33 m** | 1,8 × 0,22 m |
| `padding` | 0,03 m | 0,03 m |
| `visibleRange` | 90 m | 90 m |
| `drainSpeed` | 1,5 pruhu/s | 1,5 pruhu/s |
| `hideWhenUndamaged` | vypnuto — pruh je vidět i u nedotčeného turretu | vypnuto |
| barvy | **červená po celé délce** (`fullColor` i `midColor` čistě červené) | zelená → žlutá → červená |

Skript umí gradient zelená → žlutá → červená, scéna ho má přeladěný na červenou, takže pruh mění jen
délku. `visibleRange` 90 m je po [rozhodnutí #14](../decisions.md) **čtvrtina** dosahu turretu
(360 m) — turret střílí dávno předtím, než jeho pruh vůbec naskočí. Komponentu do scény přidává
`Tools > DroneMissile > Build Turret Health Bars`; canvas si staví sama za běhu.

## Struktura objektu `[OVĚŘENO 04.09.2026]`

Hierarchie **není** kosmetická — vynucují ji dvě věci: Unity bug s nerovnoměrným scale
([rozhodnutí #4](../decisions.md)) a konvence +Z = forward ([rozhodnutí #9](../decisions.md)). Viz
[reference/unity-gotchas.md](../reference/unity-gotchas.md).

```
turret                    ← prázdný parent, skripty EnemyTurret + TurretHealthBar sedí ZDE, tag Untagged
├── Turret_Base           ← sourozenec, tag Enemy, scale (5, 1, 5)
├── Turret_Barrel         ← sourozenec (NE child Base!), tag Enemy, scale (2,2,2), tímhle se míří
│   ├── Turret_BarrelMesh ← jen vizuál, rotace 90° X (kapsle má dlouhou osu Y)
│   └── TurretFirePoint   ← ústí, MUSÍ ležet na ose +Z hlavně
└── HealthBar             ← až za běhu; staví ho TurretHealthBar, ve scéně NENÍ
    ├── Background
    └── FillArea/Fill
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
4. **Chování po zásahu.** Zásah je od [rozhodnutí #13](../decisions.md) vidět na pruhu života, ale
   samotná likvidace pořád proběhne **bez efektu** — turret jen zmizí. Výbuch, trosky, oheň?
5. **Kolik turretů a jak rozmístěné?** Souvisí s otázkou úrovní v [concept.md](../concept.md).
6. **Má turret přestat střílet, když hráč zmizí z dosahu?** Teď při ztrátě dosahu jen přestane
   `Update` — ale cooldown běží dál, takže první výstřel po návratu do dosahu přijde okamžitě.
   `[AKTUALIZOVÁNO 05.09.2026]` Po [rozhodnutí #14](../decisions.md) je `detectionRange` 360 m, což
   pokrývá celou hratelnou plochu — z dosahu se prakticky nedá vyletět, takže otázka je zatím
   akademická. Vrátí se, až budou turretů desítky nebo bude mapa větší.
7. **Je 6% šance na zásah to, co chceme?** Nepřátelská raketa při `speed: 1080` prolétává cílem
   ([rozhodnutí #14](../decisions.md)). Teď se to hraje dobře, ale ta vzácnost je náhoda, ne design —
   spolehlivá varianta znamená sweep test místo diskrétního triggeru, tedy znovuotevřít
   [rozhodnutí #8](../decisions.md).
