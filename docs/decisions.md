# Rozhodovací log

Append-only. Jedno rozhodnutí = jeden záznam. **Záznamy se nepřepisují** — když se rozhodnutí změní,
přidá se nový, který ten starý označí za nahrazený. Smysl je nehádat se za měsíc znovu o věci,
která už padla.

Formát: **co** jsme rozhodli, **proč**, a **co to vylučuje** (to poslední je nejcennější — brání
tomu, aby někdo „vylepšil" věc, která je záměrná).

---

## #1 — Acro let, ne arkáda

**Rozhodnutí:** Letová fyzika odpovídá reálnému FPV acro módu. Žádný strop rychlosti, žádná
stabilizace, žádný auto-level.

**Proč:** Hráč je reálný FPV pilot. Hodnota hry je v tom, že let je věrný — přístupnost pro casual
hráče není cíl.

**Vylučuje:** DJI-style tlumení pohybu, auto-level, omezení maximální rychlosti, „asistované" řízení.

**Kde:** [design/flight-model.md](design/flight-model.md)

---

## #2 — Tah podél lokálního `up`, žádný samostatný move force

**Rozhodnutí:** Throttle působí čistě podél `transform.up` dronu. Pitch/roll naklápí vektor tahu, a
to je jediný zdroj horizontálního pohybu.

**Proč:** Takhle funguje reálný acro dron — dopředu se letí naklopením a přidáním gasu. Jakýkoli
přímý pohyb vpřed by ten cit zabil.

**Vylučuje:** Přidání síly ve směru `transform.forward`. Kdokoli to udělá, rozbije letový model
i když se to bude „lépe ovládat".

**Kde:** [design/flight-model.md](design/flight-model.md)

---

## #3 — Fyzické projektily, nikdy hitscan

**Rozhodnutí:** Všechna střelba na obou stranách je objekt letící prostorem.

**Proč:** Hráč musí mít šanci uhnout. Zásah má být důsledek letu, ne hodu kostkou.

**Vylučuje:** Raycast střelbu, rozptyl, přesnost v procentech, okamžité zásahy.

**Kde:** [design/weapons.md](design/weapons.md), [design/enemies.md](design/enemies.md)

---

## #4 — Turret jako sourozenci pod prázdným parentem

**Rozhodnutí:** `Turret_Base` a `Turret_Barrel` jsou sourozenci pod prázdným objektem `turret`,
který nese skript `EnemyTurret`.

**Proč:** Základna má nerovnoměrný scale `(1.5, 0.3, 1.5)`. Rotace hlavně jako childa takového
parenta mesh vizuálně deformovala.

**Vylučuje:** „Uklizení" hierarchie tak, že hlaveň se stane childem základny. Vypadá to čistěji
a je to rozbité.

**Důsledek:** Skript je na jiném objektu než collidery → `RocketProjectile` musí používat
`GetComponentInParent`, ne `GetComponent`.

**Kde:** [reference/unity-gotchas.md](reference/unity-gotchas.md)

---

## #5 — Turrety mají HP, ne one-shot kill

**Rozhodnutí:** Turret má 30 HP, raketa hráče dává 10 → tři zásahy.

**Proč:** Z boje se stává manévr — hráč musí zůstat v akci, přiletět víckrát. One-shot kill by z
toho udělal reflexovou hru.

**Vylučuje:** Instant kill zbraně.

**Kde:** [design/enemies.md](design/enemies.md)

---

## #6 — Vysílačka se čte přímo, ne přes Input Actions

**Rozhodnutí:** Kód čte `Joystick` zařízení a jeho controly přímo, i když v projektu je
`InputSystem_Actions.inputactions`.

**Proč:** Vysílačka se hlásí jako generický HID se zanořenými a nestandardními cestami os
(`stick/y`). Přímé čtení je u téhle třídy zařízení spolehlivější než abstrakce nad actions.

**Vylučuje:** Nic natrvalo — je to volba pro tuhle fázi. Přechod na actions je otevřená otázka
v [design/controls.md](design/controls.md).

**Cena:** Žádný rebinding za běhu, mapování je zadrátované v kódu na dvou místech.

---

## #7 — Diagnostické logování zařízení zůstává v kódu

**Rozhodnutí:** `DroneControls.Start()` při každém spuštění loguje všechna zařízení a všechny jejich
controly (name / path / type).

**Proč:** Input Debugger nezobrazuje plné cesty potřebné pro kód. Tenhle výpis je jediný způsob, jak
zjistit mapování pro novou vysílačku — je to nástroj, ne zbytek po debugování.

**Vylučuje:** Smazání toho logu jako „cleanup".

**Kde:** [reference/radiomaster-pocket.md](reference/radiomaster-pocket.md)

---

## #8 — Zásah projektilu se detekuje triggerem, ne kolizí

**Rozhodnutí:** Oba projektily zůstávají **kinematické** a jejich collider je **trigger**. Zásah
řeší `OnTriggerEnter`, pohyb je `rb.MovePosition` ve `FixedUpdate`, a vlastního střelce filtruje
explicitní pole `owner` předané při `Instantiate`.

**Proč:** Kinematické Rigidbody negeneruje `OnCollisionEnter` proti statickým colliderům, a všechny
collidery turretu statické jsou — proto rakety hráče procházely turretem (bug #1). Kinematické
těleso s **trigger** colliderem ale trigger zprávy proti statickým colliderům generuje.
Alternativa „plná fyzika" by fungovala taky, jenže dron má `mass 0.0075` a projektil `mass 1` —
každý zásah by dronem odstřelil. Trigger dává detekci se **nulovou fyzikální reakcí**, což je přesně
to, co dosavadní transform-driven model měl.

**Vylučuje:**

- Spoléhání na `OnCollisionEnter` u projektilů. Vypadá to jako správná Unity cesta a tiše nefunguje.
- Přidávání CCD (`Continuous`, `ContinuousDynamic`) jako řešení prolétávání — na kinematickém
  tělese je to no-op.
- Přesun pohybu zpátky do `Update()`. Tam posun závisí na frameratu a při ~30 fps začne přesahovat
  délku collideru, čímž se prolétávání vrátí.
- `speed` nad **~49 m/s** bez znovuotevření tohoto rozhodnutí. Záruka „nic nepropadne" stojí na tom,
  že posun za fyzikální krok (`speed × 0,02`) je menší než délka collideru projektilu (0,986 m).
- Návrat k `Physics.IgnoreCollision` na tag jako filtru self-hitu. Nepokrývalo to `turret` root,
  který je `Untagged`, má vlastní collider a spawnuje se na něm nepřátelská raketa.

**Cena:** Tag `Enemy` tím ztratil jediné použití v kódu — zůstává ve scéně i v `TagManager.asset`
jako sémantické značení, ale nic ho už nečte. Prefaby v Inspectoru dál ukazují `Is Kinematic` a
`Continuous`; autoritativní je kód ve `Start()`.

**Kde:** [design/weapons.md](design/weapons.md), [reference/unity-gotchas.md](reference/unity-gotchas.md)
