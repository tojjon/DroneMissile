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

---

## #9 — +Z je forward, a míří se z ústí

**Rozhodnutí:** Napříč projektem platí **+Z = forward**. Mesh, který tu konvenci nesplňuje, se
izoluje do **rotovaného child objektu**, ne do kompenzační rotace v kódu. A směr míření se počítá
z **`firePoint.position`** (ústí), ne z `barrel.position` (pivot).

**Proč:** Bug #2 byly dvě vady se stejnou příčinou. Unity primitivy (`Capsule` = mesh `10208`,
`Cylinder`) mají dlouhou osu **Y**, ale `Quaternion.LookRotation` zarovnává **+Z** — hlaveň proto
mířila na hráče bokem. A ústí bylo na `(0, 0.64, 0.568)` × scale hlavně 2, tedy **1,28 m mimo osu
míření**: střela letěla souběžně s osou míření, ale trvale o 1,28 m vedle, konstantně na jakoukoli
vzdálenost. Dron má collider vysoký 0,387 m, takže nepřátelské projektily ho nemohly zasáhnout
nikdy. Ten `y: 0.64` tam byl přesně proto, že někdo dal ústí na viditelný hrot té svislé kapsle —
jedna neshoda konvence, dva symptomy.

**Vylučuje:**

- Kompenzační rotaci v kódu míření (`LookRotation(dir) * Quaternion.Euler(90, 0, 0)`). Udělá
  z „barrel forward" směr **dolů** a rozbije `firePoint.rotation`, na kterém stojí `Fire()`. Je to
  přesně ten typ skryté vazby, který způsobil bug #1.
- Vrácení `MeshFilter`/`MeshRenderer` zpátky na aim transform (`Turret_Barrel`). Vizuál patří do
  childa `Turret_BarrelMesh` s rotací 90° X.
- `TurretFirePoint` mimo osu **+Z** hlavně. Jakákoli složka X/Y na něm vrací systematické míjení.
- Míření z `barrel.position`.
- Nenulovou lokální rotaci na `TurretFirePoint` — `Fire()` spoléhá na to, že
  `firePoint.rotation == barrel.rotation`.

**Cena:** Míření z ústí je fixed-point iterace (otočením hlavně se ústí posune). Konverguje za snímek
dva a `RotateTowards` to stejně rate-limituje, ale exaktní uzavřené řešení to není.

**Kde:** [design/enemies.md](design/enemies.md), [reference/unity-gotchas.md](reference/unity-gotchas.md)

---

## #10 — Dron nemá HP; dotek země restartuje scénu

**Rozhodnutí:** `DroneHealth` je **smazaná**. Dron nemá žádné zdraví. Místo toho:

- **Jakýkoli kontakt s pevným objektem** (terén, plošina, turret) **znovu načte scénu.**
  `DroneControls.OnCollisionEnter` → `SceneManager.LoadScene(GetActiveScene().buildIndex)`.
- **Zásah nepřátelskou raketou bere na 1 s ovládání** — `DroneControls.Stun()`. Bez tahu a bez
  `MoveRotation` dron drží poslední náklon a padá. Nízko nad zemí je to jistý pád, výš se to dá
  vybrat.

**Proč:** Tohle odpovídá na otevřenou otázku #3 v [concept.md](concept.md) („umírá hráč, a co pak?"),
která blokovala i bug #3 v [backlogu](backlog.md) — komponenta nešla jen tak přidat, dokud nebylo
jasné, co má na 0 HP nastat. Odpověď je, že se HP model nedodělává, ale ruší. Sedí to na pilíř
z [conceptu](concept.md): letová fyzika je základ, střílení nadstavba. Dovednost je let, ne
odolnost — jedna chyba stojí run. Nepřátelský oheň tím zůstává hrozbou (stun sráží dron k zemi),
aniž by potřeboval druhý, paralelní model poškození.

**Vylučuje:**

- `DroneHealth`, health bar, damage čísla na dronu, regeneraci.
- Crash damage při nárazu do terénu (byl to zaparkovaný nápad v [backlogu](backlog.md) — tohle ho
  nahrazuje: náraz není poškození, je to konec runu).
- Respawn na místě, checkpointy, „životy". Restart je celá scéna, včetně HP turretu.
- Rozlišování „tvrdý náraz vs. jemné přistání". Dotek je dotek — přistání je taky restart.
- Vracení `damage` pole na `EnemyProjectile`. Nahradilo ho `stunDuration`.

**Důsledek — tři věci v kódu, které vypadají jako zbytečné a nejsou:**

1. **Arming latch (`armAltitude`).** Dron spawnuje **položený na plošině**, takže úplně první kontakt
   by přišel v čase 0 a scéna by se načítala pořád dokola. Kolize se počítá až potom, co dron
   vystoupá `armAltitude` nad spawn. Grace *timer* na tomhle místě funguje jen náhodou a varianta
   „nahodit, až hráč přidá plyn" se nahodí okamžitě, když throttle stick vysílačky není dole —
   `(rawThrottle + 1) / 2` není nikdy záporné.
2. **Kill plane (`killY`).** Plošina je **plovoucí** deska 23 × 29 m, terén je 57 m pod ní. Sjetí
   přes okraj v malé výšce je pád, který kolizní cesta může minout, a v projektu neexistuje žádný
   restart input — byl by to neřešitelný soft-lock. Proto se `killY` **nekontroluje** proti
   arming latchi.
3. **I-frames (`stunImmunity`).** Scéna má na turretu `fireRate: 0.1`, tedy 10 výstřelů za sekundu.
   Bez i-frames by se stun pořád obnovoval a „vlétnout do dosahu" by znamenalo být bez ovládání až
   do dopadu.

**Cena:** Restart scény vrací turretu HP na 30 — každý pád maže celý postup v boji. Žádné
checkpointy. A `DroneControls.Start()` podle [rozhodnutí #7](decisions.md) vypisuje všechna zařízení
a všechny jejich controly; ten výpis se od teď spouští po **každém** pádu, což v editoru zahltí
konzoli. Log zůstává (#7 platí), ale jeho cena se tímhle rozhodnutím změnila.

**Kde:** [concept.md](concept.md), [design/flight-model.md](design/flight-model.md)

---

## #11 — Dron používá CCD; výjimka z #8 platí jen pro projektily

**Rozhodnutí:** `DroneControls.Start()` nastavuje dronu
`collisionDetectionMode = ContinuousDynamic` a `interpolation = Interpolate`.

**Proč:** [Rozhodnutí #8](decisions.md) říká, že CCD je no-op — ale to platí **na kinematickém
tělese**, tedy u projektilů. Dron je dynamický a CCD na něm funguje normálně. S `Discrete` collider
vysoký 0,387 m při kroku 0,02 s **proletí terénním heightfieldem nad ~19,4 m/s**, a pouhé sjetí
z okraje plošiny dopadá rychlostí ~25 m/s. Terminální rychlost volného pádu je tu 32,7 m/s a
s plným plynem dolů se dá dostat výš. Když každá minutá kolize znamená nezachycený pád do prázdna
(viz #10), Discrete není použitelný.

**Vylučuje:**

- „Úklid" CCD na dronu s odkazem na #8. **#8 se týká projektilů, ne dronu.**
- `ContinuousSpeculative`. Nesweepuje, jen nafukuje contact offsety, a na sousedních trojúhelnících
  heightfieldu generuje fantomové kontakty. Tady je jeden fantomový kontakt tichý restart scény.
- Spoléhání na to, že CCD pokryje i rotaci. `MoveRotation` je rotační teleport a **nesweepuje se**.
  Při dnešních 100 °/s jsou to 2° za krok a nevadí to; kdyby se rate zvedly k reálným acro
  hodnotám (800–1200 °/s), rotující dron může tenkou hranou projít.

**Cena:** Jeden sweep za fyzikální krok pro jedno těleso — neměřitelné. `Interpolate` je tu navíc
zadarmo výhra: `Main Camera` je child dronu, takže se přestane trhat, když render fps přesáhne 50.

**Kde:** [design/flight-model.md](design/flight-model.md), [reference/unity-gotchas.md](reference/unity-gotchas.md)

---

## #12 — Smrt má prodlevu a hlášku; stun je vidět jako červený rám, střed má zaměřovač

**Rozhodnutí:** Přidán první HUD — `DroneHUD` (`Assets/scripts/DroneHUD.cs`). Zobrazuje přesně tři
stavy, které ve hře už existují, a nic dalšího:

1. **Stun** — po celou dobu, kdy platí `DroneControls.IsStunned`, se u okrajů obrazovky rozsvítí
   měkký červený vignette rám. Náběh rychlý, doběh pomalejší, s jemným pulzem.
2. **Smrt** — `Crash()` už **nevolá `SceneManager.LoadScene` na místě**. Nastaví `hasCrashed`
   a deadline `reloadAt = Time.time + deathDelay` (výchozí 1 s); samotný reload provede
   `FixedUpdate`. Po tu sekundu je uprostřed obrazovky hláška `YOU DIED` a **fyzika běží dál** —
   vrak pod textem dopadá a kutálí se.
3. **Zaměřovač** — malý křížek s prázdným středem uprostřed obrazovky, staticky. Ukazuje, kam
   letí raketa. Mizí při smrti (vyfaduje přesně tak, jak naskakuje `YOU DIED`), při stunu
   **zůstává svítit**.

Tím se zavírá navazující otevřená otázka #3 v [concept.md](concept.md): restart **není** němý.

**Proč:**

- Stun byl na obrazovce k nerozeznání od „ovládání se rozbilo". Dron prostě přestal reagovat a
  padal, bez jakékoli zpětné vazby, že za to může zásah.
- Okamžitý střih při pádu neukázal, co hráče zabilo. Sekunda navíc s běžící fyzikou to ukáže.
- `Time.timeScale` se **nemění**. Celá třída jede na `Time.time` a nenulový timescale, který
  přežije do další scény, je klasický způsob, jak rozbít reload.
- HUD si canvas **staví sám v kódu**, nesedí v `SampleScene.unity`. Scény se v tomhle repu needitují
  jako YAML (viz [CLAUDE.md](../CLAUDE.md)), vignette je stejně generovaná textura, takže by vznikala
  za běhu tak jako tak, a takhle je celá featura v jednom čitelném souboru místo neprůhledného diffu
  scény. Objekt `HUD` do scény přidává editorové tlačítko `Tools > DroneMissile > Build HUD`
  (`Assets/Editor/HudBuilder.cs`) — stejný postup jako `TurretBodyBuilder` u turretu.
- Text je `UnityEngine.UI.Text` s vestavěným fontem `LegacyRuntime.ttf`, ne TextMeshPro. TMP by
  vyžadovalo naimportovat TMP Essential Resources, tedy ~2 MB assetů navíc v repu kvůli dvěma slovům.
- **Statický křížek uprostřed je tady doopravdy správně, ne přibližně.** `Main Camera` a `FirePoint`
  jsou sourozenci pod dronem a mají **bit po bitu stejnou** lokální rotaci
  (`{x: -0.21643952, y: 0, z: 0, w: 0.97629607}`, tedy -25° pitch); dron sám je nenatočený. Úhlová
  odchylka mezi optickou osou a osou hlavně je **přesně 0,000°** a boční offset je **nulový**. Ústí
  sedí 4,15 mm pod optickou osou, což je chyba 0,024° na 10 m a míň dál — pod desetinu pixelu na
  1080p. Raketa navíc letí po **dokonalé přímce** (kinematická, bez gravitace, konstantní rychlost),
  takže není co dopočítávat.

**Vylučuje:**

- **Všechno, co vylučuje [rozhodnutí #10](decisions.md), platí dál.** Žádné HP, health bar, damage
  numbers, životy, respawn ani checkpointy. HUD jen zobrazuje stav, který už existuje.
- Rám u zásahu, který zahodí i-frames. `stunImmunity` (2 s) znamená, že další zásahy stun
  nezpůsobí, a rám proto zůstane tmavý. Je to záměr: rám má jediný význam — „teď nemáš ovládání".
  `Stun()` tedy nemusí vracet `bool` ani vysílat event.
- Event bus nebo `UnityEvent` na stun a smrt. `DroneHUD` si stav **pollnuje** v `Update()` přes
  `IsStunned` / `HasCrashed`. Žádný event bus v projektu není a kvůli dvěma boolům se nevyplatí.
- `EventSystem` a `GraphicRaycaster`. HUD nebere input a `raycastTarget` je všude vypnutý.
- Restart input. Pořád neexistuje — hláška je informativní, není to „retry" obrazovka s tlačítkem.
- Zveřejnění `stunnedUntil` / `hasCrashed` jako polí. Ven jdou jen read-only property `IsStunned`
  a `HasCrashed`; `Crash()` zůstává jediné místo, které stav pádu nastavuje.
- **Jakýkoli výpočet dopadu u zaměřovače.** Žádný raycast, žádný world-space marker, žádné
  předsazení („lead") ani balistika. Osy jsou rovnoběžné a raketa nemá gravitaci — střed obrazovky
  **je** bod dopadu na libovolnou vzdálenost. Kdyby se to někdy „opravilo" raycastem, je to regrese.
  Přestane to platit jedině tehdy, když někdo ve scéně rozladí rotaci `FirePoint` proti kameře.
- Indikace cooldownu na zaměřovači. `fireRate` je 0,5 s, ale křížek je **statický** — má jediný
  význam, „sem to poletí".
- Ztmavení křížku při stunu. `Shoting.Update()` se `DroneControls` **vůbec neptá** a hlídá jen
  `Time.time >= nextFireTime`, takže se během stunu opravdu střílet dá. Tmavší křížek by lhal.
  (Jestli se za stunu střílet *má*, je otevřená otázka — ale je to současné chování, ne tohle
  rozhodnutí.)

**Cena:** Mezi nárazem a reloadem je sekunda, kdy fyzika běží a hráč nemá ovládání; po pádu z výšky
se vrak ještě kus veze. Zásahy během té sekundy jsou neškodné — `Stun()` i `OnCollisionEnter` se
o `hasCrashed` opírají a znovu nespustí, a kill plane se testuje až za tím returnem. Vignette
textura se generuje jednou ve `Start()` podle poměru stran obrazovky; při změně rozlišení za běhu
se nepřegeneruje a rám na okrajích zesílí.

**Kde:** [concept.md](concept.md), [../CLAUDE.md](../CLAUDE.md)
