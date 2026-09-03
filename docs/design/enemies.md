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

## Parametry `[OVĚŘENO 03.09.2026]`

| Parametr | Hodnota |
|---|---|
| `detectionRange` | 30 |
| `fireRate` | 2 s |
| `turnSpeed` | 90 °/s |
| `maxHealth` | 30 |
| projektil `speed` / `lifeTime` / `damage` | 20 / 5 s / 10 |

Turret je zatím jediný ve scéně a je jediná věc ve hře, která **umí zemřít** (`Destroy(gameObject)`).

## Struktura objektu `[OVĚŘENO 03.09.2026]`

Hierarchie **není** kosmetická — vynucuje ji Unity bug s nerovnoměrným scale, viz
[reference/unity-gotchas.md](../reference/unity-gotchas.md) a [rozhodnutí #4](../decisions.md):

```
turret            ← prázdný parent, skript EnemyTurret sedí ZDE, tag Untagged, má CapsuleCollider
├── Turret_Base   ← sourozenec, tag Enemy
├── Turret_Barrel ← sourozenec (NE child Base!), tag Enemy, tímhle se míří
└── TurretFirePoint
```

Tag `Enemy` je na `Turret_Base` **i** `Turret_Barrel` (poznámky ze session zmiňovaly jen `Base`).

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
