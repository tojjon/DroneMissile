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

---

## #13 — Turret má health bar nad sebou, ve world space

**Rozhodnutí:** Nad turretem visí pruh života. Nová komponenta `TurretHealthBar`
(`Assets/scripts/TurretHealthBar.cs`) sedí na **stejném objektu jako `EnemyTurret`**, tedy na
prázdném rootu `turret`. Canvas si staví sama v kódu jako `DroneHUD`, ale je **world space**, ne
screen-space overlay:

- Ve scéně **2,7 × 0,33 m, 4,8 m nad rootem** (default ve skriptu je 1,8 × 0,22 m / 3,2 m), tmavý
  rám a barevná výplň.
- **Billboard** — každý `LateUpdate` kopíruje rotaci kamery, takže pruh zůstává rovnoběžný
  s plochou obrazovky.
- Výplň se hýbe **anchory** (`anchorMax.x` v odsazeném childu `FillArea`), ne `Image.Type.Filled`.
- Gradient je ve skriptu zelená → žlutá → červená; **scéna ho má přeladěný na červený** (`fullColor`
  i `midColor` jsou čistá červená, `lowColor` tmavší), takže pruh nemění barvu, jen délku. Zobrazená
  hodnota dojíždí na `drainSpeed` (1,5 pruhu/s).
- Dál než `visibleRange` (90 m) se pruh vypne. Po přeladění v [rozhodnutí #14](decisions.md) je to
  **hluboko pod** `detectionRange` (360 m) — turret začne střílet dávno předtím, než je jeho pruh
  vidět.
- `EnemyTurret` k tomu dostal read-only property `MaxHealth` / `CurrentHealth` / `HealthFraction`
  a inicializace HP se přesunula ze `Start()` do `Awake()`.
- Do scény komponentu přidává `Tools > DroneMissile > Build Turret Health Bars`
  (`Assets/Editor/TurretHealthBarBuilder.cs`) — stejný postup jako `HudBuilder` a `TurretBodyBuilder`.

**Proč:**

- HP turretu z [rozhodnutí #5](decisions.md) byly doteď vidět **jen v konzoli** přes `Debug.Log`
  v `TakeDamage()`. Pravidlo „víc zásahů, žádný one-shot kill" má pro hráče smysl teprve tehdy,
  když je ten postup vidět — jinak neví, jestli zásah sedl, ani kolik ještě zbývá. Po
  [rozhodnutí #14](decisions.md) je zásahů deset, takže pruh je tím spíš nutný.
- **World space, ne HUD.** Pruh patří konkrétnímu turretu: musí ho zakrývat terén, musí se zmenšovat
  s dálkou a při víc turretech musí sedět nad tím správným. Ve screen-space overlay by se každá
  pozice musela promítat do obrazovky a zákryt terénem by se řešil zvlášť.
- **Na rootu `turret`, ne na `Turret_Barrel`.** Hlaveň je aim transform a otáčí se
  ([rozhodnutí #4](decisions.md), [#9](decisions.md)); pruh na ní by kroužil kolem hlavy.
- **`Awake()` místo `Start()`** u `currentHealth`: pořadí `Start()` dvou komponent na jednom objektu
  není definované, `Awake()` je vždycky dřív. Bez toho by pruh první frame ukázal prázdno.
- **`LateUpdate`, ne `Update`.** Billboard musí přijít po všem, co v daném framu hýbe kamerou, jinak
  pruh o frame zaostává za rotací dronu.
- Bezspritový `Image` kreslí plný quad, takže na pruh nepotřebuje projekt žádnou texturu ani sprite —
  na rozdíl od vignette v [rozhodnutí #12](decisions.md) tady nic negenerujeme.

**Vylučuje:**

- **HP nebo health bar pro dron. [Rozhodnutí #10](decisions.md) a [#12](decisions.md) platí dál.**
  Tohle je pruh **turretu**, který HP má od #5. Není to otevření damage modelu dronu.
- Čísla, procenta a damage numbers. Pruh a nic víc. `Debug.Log` v `TakeDamage()` zůstává, ale je pro
  vývoj, ne pro hráče.
- Text nad turretem (jméno, HP číslo) a s ním TextMeshPro. Stejný důvod jako u #12 — TMP Essential
  Resources se do repa netahá.
- Event bus nebo `UnityEvent` na damage. `TurretHealthBar` si stav **pollnuje** přes
  `HealthFraction`, stejně jako `DroneHUD` pollnuje `IsStunned` / `HasCrashed`.
- Zveřejnění `currentHealth` jako pole. Ven jdou jen read-only property; `TakeDamage()` zůstává
  jediné místo, které HP mění.
- Škálování na konstantní velikost na obrazovce. Pruh je world-space objekt a zmenšuje se s dálkou —
  na hranici viditelnosti (90 m) je z těch 2,7 m asi 19 px na 1080p při FOV 80, což ještě stačí.
  Konstantní velikost by znamenala buď pruh přes půl obrazovky z blízka, nebo clamp a další ladicí
  konstantu.
- `EventSystem`, `GraphicRaycaster` a `worldCamera` na canvasu. Pruh nebere input, `raycastTarget` je
  všude vypnutý.

**Cena:** Každý turret si drží vlastní world-space Canvas — vlastní draw call, a canvas se
rebuilduje po celou dobu, kdy výplň dojíždí. Při jednom turretu je to šum, při desítkách by se to
mělo změřit. `hideWhenUndamaged` je **vypnuté**, takže nedotčený turret je vidět z 90 m — jenže po
[rozhodnutí #14](decisions.md) hráče sám vidí z 360 m, takže pruh jeho polohu neprozradí, naopak
hráč schytá první rány dřív, než se má čeho chytit. Kdo to chce jinak, zvedne `visibleRange`.
Rozměry jsou v metrech a předpokládají, že root `turret` má
scale 1 (což má); pod naškálovaným parentem by se pruh zvětšil s ním.

**Kde:** [design/enemies.md](design/enemies.md), [../CLAUDE.md](../CLAUDE.md)

---

## #14 — Přeladění boje: dálkový turret, hyperrychlá nepřátelská raketa

**Rozhodnutí:** Boj je přeladěný na hodnoty, které vzešly z hraní, a **dokumentace se srovnává se
scénou**, ne naopak. Platné hodnoty (Inspector overrides ve `SampleScene.unity`
a v `enemy_rocket.prefab`) `[OVĚŘENO 05.09.2026]`:

| Kde | Parametr | Bylo | Je |
|---|---|---|---|
| `EnemyTurret` (scéna) | `detectionRange` | 60 | **360** |
| `EnemyTurret` (scéna) | `fireRate` | 0,1 s | **0,2 s** (5 ran/s) |
| `EnemyTurret` (scéna) | `turnSpeed` | 90 °/s | **720 °/s** |
| `EnemyTurret` (scéna) | `maxHealth` | 30 | **100** → 10 zásahů |
| `EnemyProjectile` (prefab) | `speed` | 60 | **1080** |
| `DroneControls` (scéna) | `stunImmunity` | 2 s | **0,5 s** |

**Proč:** Je to ladění podle pocitu ze hry, ne odvození z modelu. Turret, který vidí přes celou
mapu, otáčí se prakticky okamžitě a střílí bleskovou střelou, drží hráče pod tlakem po celý let
místo krátkého okna kolem věže; deset zásahů místo tří dělá z likvidace turretu úkol na víc náletů,
což je přesně to, co chtělo [rozhodnutí #5](decisions.md); a kratší i-frames (0,5 s) k tomu sedí,
protože turret při `fireRate: 0.2` už nestřílí 10 ran za sekundu.

**Vylučuje:**

- **„Opravu" těchhle čísel zpátky na defaulty ve skriptech.** `EnemyTurret.cs` má pořád defaulty
  30 / 2 s / 90 / 30 a `EnemyProjectile.cs` `speed = 20` — autoritativní je scéna a prefab. Nový
  turret přetažený do scény tyhle hodnoty **nedostane**, musí se nastavit ručně.
- Tabulky v [design/enemies.md](design/enemies.md) a [design/weapons.md](design/weapons.md) jako
  zdroj pravdy o defaultech skriptu. Popisují scénu.
- Technický dluh „`fireRate` ve scéně nesouhlasí s docs" z [backlog.md](backlog.md). Tímhle je
  uzavřený — vyhrála scéna.

**Cena — a je velká: `speed: 1080` prolomil strop z [rozhodnutí #8](decisions.md).**

Detekce zásahu je diskrétní: projektil se každý fyzikální krok přesune o `speed × 0,02`. Při
1080 m/s je to **21,6 m za krok**, zatímco collider projektilu je 0,986 m dlouhý a collider dronu
má 0,387 m. Testované pozice se tedy nepřekrývají — mezi dvěma vzorky zůstane přes 20 m slepé
mezery. Nepřátelská raketa proto dron **většinou mine i při přímém zásahu**: trefí jen tehdy, když
některý vzorek náhodou padne do okna zhruba 1,4 m z každých 21,6 m, tedy **kolem 6 % letů**.
Prolétává i terénem, a `lifeTime: 5` s jí při té rychlosti dává dolet 5,4 km.

Je to **vědomá volba** — takhle se hra hraje dobře a stun je vzácný, spíš překvapení než trest.
Ale je to *náhodná* vzácnost, ne navržená: nedá se ladit, hráč ji nemůže číst a zmizí, jakmile
někdo sáhne na `Fixed Timestep` nebo na délku collideru. Kdyby se místo toho chtěl **spolehlivý**
zásah, cesta není snížit `speed` naslepo, ale znovu otevřít #8 (sweep test / raycast po dráze mezi
dvěma kroky místo diskrétního triggeru) — pak může být raketa rychlá i spolehlivá zároveň.

Strop **~49 m/s dál platí pro raketu hráče** (`rocket.prefab`, `speed: 30`). Tam se na spolehlivé
detekci stojí — je to jediný způsob, jak turret zabít.

Vedlejší cena: `detectionRange: 360` pokrývá celou hratelnou plochu, takže „vyletět z dosahu" už
prakticky neexistuje a otevřená otázka #6 v [design/enemies.md](design/enemies.md) (cooldown běžící
mimo dosah) je tím z velké části bezpředmětná. A `visibleRange` pruhu života
([rozhodnutí #13](decisions.md)) zůstal na 90 m, tedy čtvrtina dosahu turretu.

**Kde:** [design/enemies.md](design/enemies.md), [design/weapons.md](design/weapons.md),
[reference/unity-gotchas.md](reference/unity-gotchas.md), [backlog.md](backlog.md),
[../CLAUDE.md](../CLAUDE.md)

---

## #15 — Turret střílí okamžitě po návratu hráče do dosahu (záměr)

**Rozhodnutí:** Cooldown turretu běží na reálném čase i ve chvíli, kdy je hráč mimo
`detectionRange`. Hráč, který odletí a vrátí se po víc než `fireRate` sekundách, dostane výstřel
**okamžitě, bez náběhu** — a tak to má být. `[ROZHODNUTO 05.09.2026]`

**Proč:** Ověřeno hraním, stejně jako [rozhodnutí #14](decisions.md) — takhle se to hraje dobře.
Návrat do dosahu je hráčovo rozhodnutí, a okamžitá odpověď z něj dělá rozhodnutí s cenou. Náběh po
návratu by z každého vyklonění udělal bezpečné okno, což je přesně opačný pocit, než jaký turret má
mít.

Kód se tím **nemění.** `EnemyTurret.Update()` se při hráči mimo dosah ukončí před kontrolou
`nextFireTime` a `nextFireTime` se nikde neresetuje — to chování je od teď specifikace, ne shoda
okolností.

**Vylučuje:**

- Reset nebo clamp `nextFireTime` při ztrátě cíle (`nextFireTime = Time.time + fireRate` v místě
  toho `return`) — to je ta „oprava", které se tímhle zavírají dveře.
- „Náběh" / telegrafování prvního výstřelu po zaměření: prodleva na akvizici cíle, zvuk nebo
  animace před první ranou.
- Položku „4. Turret vystřelí okamžitě při návratu hráče do dosahu" v [backlog.md](backlog.md) jako
  bug. Zavřená.
- Otevřenou otázku #6 v [design/enemies.md](design/enemies.md) („Má turret přestat střílet, když
  hráč zmizí z dosahu?"). Zodpovězená: cooldown běží dál.

**Souvislost, na kterou si dát pozor:** dnes je to skoro neviditelné, protože `detectionRange` je po
[rozhodnutí #14](decisions.md) 360 m a pokrývá celou hratelnou plochu — z dosahu se prakticky nedá
vyletět. **Naostro se to projeví, až bude ztráta cíle běžná**, a hlavně kdyby se přidal line-of-sight
(otázka #2 v [design/enemies.md](design/enemies.md)): pak by každé vyklonění zpoza kopce znamenalo
ránu v tomtéž okamžiku a terén jako kryt by byl výrazně tvrdší, než jak ten nápad zní. Tohle
rozhodnutí říká, že to je žádoucí — ne že se na to zapomnělo.

**Kde:** [design/enemies.md](design/enemies.md), [backlog.md](backlog.md)

---

## #16 — VFX se staví v kódu; oblouky nejsou na trupu, ale před kamerou

**Rozhodnutí:** Zásah rakety hráče dělá krátký červený výbuch, stun dronu doprovázejí elektrické
oblouky. Obojí se staví **v C# za běhu** — žádný prefab, žádná `.mat`, žádná `.png`, žádný VFX
Graph. Materiál vzniká přes `Shader.Find("Universal Render Pipeline/Particles/Unlit")` a blend state
se nastavuje ručně. Oblouky se kreslí v **boxu před kamerou**, ne na trupu dronu.
`[ROZHODNUTO 05.09.2026]`

**Proč (stavba v kódu):** Stejný důvod jako u [rozhodnutí #12](decisions.md) a
[#13](decisions.md) — scény ani prefaby se v tomhle repu needitují jako YAML, takže efekt jako asset
by znamenal ruční zásah do scény plus `.meta` navíc. V kódu je celá věc jeden čitelný soubor.
Textury se generují stejně jako vigneta v `DroneHUD.BuildVignetteSprite()`: bílá RGB, tvar v alfě,
takže `Color` v Inspectoru tinktuje živě.

**Proč (ruční blend state):** URP nastavuje blend mód z **editor-only** ShaderGUI
(`BaseShaderGUI.SetupMaterialBlendMode`), který za běhu neexistuje. `ParticlesUnlit.shader` ale čte
`Blend[_SrcBlend][_DstBlend] ZWrite[_ZWrite]` přímo z materiálu, takže aditivní míchání zapnou ty
floaty, ne keyword. `FxAssets.BuildAdditive()` je ta metoda přepsaná ručně.

**Proč (oblouky před kamerou, ne na trupu):** `Main Camera` sedí na drone-local `(0, 0.12, 0.629)`
s near clipem 0,3 m, zatímco tělo dronu je krychle 1×1×1 vycentrovaná v počátku (world extents
±(0,31, 0,19, 0,5)). Kamera je tedy **0,129 m před špičkou** a celý trup leží za rovinou kamery —
nejbližší roh vychází skalárním součinem na −0,015. Oblouky „na trupu" by v first person
**nebylo nikdy vidět**. `fieldCenter` / `fieldExtents` jsou proto drone-local box před objektivem;
kdyby někdy přibyla chase kamera, stačí `fieldCenter` vynulovat a efekt sedí na trupu.

**Proč (rig není child dronu):** Root dronu má scale `(0.61325, 0.38720787, 1)`. Child by to
zploštění zdědil — je to tatáž past jako [rozhodnutí #4](decisions.md) — a u `LineRenderer` i
`ParticleSystem` se šířka a velikost počítají z lossy scale. `DroneStunArcs` proto vyrábí
**neparentovaný root objekt se scale 1** a v `LateUpdate` mu kopíruje pozici a rotaci dronu.
`TurretHealthBar` si child dovolit může, protože root turretu scale 1 má.

**Proč (pollování `IsStunned`):** Není to jen pohodlí — je to to, co drží pravidlo. `Stun()` se při
i-frames (`stunImmunity`) vrátí dřív, než sáhne na `stunnedUntil`, takže odmítnutý zásah nechá
`IsStunned` na `false` a oblouky se nespustí. Přesně stejné pravidlo jako červený rám HUD v
[rozhodnutí #12](decisions.md), a jedou tím pádem na stejný takt.

**Vylučuje:**

- Instalaci VFX Graphu (`com.unity.visualeffectgraph`). Vestavěný `ParticleSystem` stačí.
- Commitnutí particle prefabu, materiálu nebo textury pro efekty.
- Pooling výbuchů. `Shoting.fireRate` je 0,5 s, takže špička jsou dva výbuchy za sekundu; jediná
  alokace, na které záleželo, je materiál a textura, a ty sdílí `FxAssets` staticky.
- Parentování efektů pod `Drone` (scale výš) nebo `FirePoint` (scale `(1.63, 2.58, 1)`).
- Spuštění efektu stunu na místě zásahu nepřátelské rakety. Ta má po
  [rozhodnutí #14](decisions.md) `speed: 1080`, takže trigger padne ~21 m za dronem a efekt by se
  objevil v prázdném vzduchu. Kotví se na dron.
- `AddComponent<DroneStunArcs>()` z `DroneControls.Start()`. Byla by to nulová změna scény, ale
  drátovalo by to view kód do jediné fyzikální komponenty.
- Výbuch jen na zemi. Střílí se při **každém** zásahu — terén, deska, turret.

**Cena:** `Shader.Find` vidí jen shadery, na které se v buildu někdo odkazuje, a v tomhle projektu
nikdo. V editoru to funguje vždycky, v player buildu vrátí `null`, dokud shader nebude v
*Project Settings > Graphics > Always Included Shaders*. `FxAssets` na to hlásí warning a padá zpět
na `Sprites/Default`, takže selhání je hlasité, ne magenta. Zapsáno v [backlog.md](backlog.md).

**Kde:** [design/weapons.md](design/weapons.md), [reference/unity-gotchas.md](reference/unity-gotchas.md),
[backlog.md](backlog.md), [../CLAUDE.md](../CLAUDE.md)

---

## #17 — Raketa hráče míří sweep testem; blesky jsou částice ve world space

**Rozhodnutí:** Zásah rakety hráče se detekuje **raycastem po úseku mezi dvěma fyzikálními kroky**,
ne diskrétním triggerem. Elektrický efekt při stunu jsou **částice simulované ve world space**
(noise + trails), ne `LineRenderer` geometrie. A HDR barva se nastavuje **na materiálu**, nikdy na
`startColor`. `[ROZHODNUTO 05.09.2026]`

**Proč (sweep):** `rocket.prefab` má ve skutečnosti `speed: 120` — ne 30, jak tvrdily
[design/weapons.md](design/weapons.md) i `CLAUDE.md`. To je **2,4 m za fyzikální krok** proti
collideru 0,986 m, takže se testované pozice nepřekrývají a raketa **prolétla zhruba 59 % zásahů**
bez jediného triggeru: bez poškození, bez logu, bez výbuchu. Strop ~49 m/s z
[rozhodnutí #8](decisions.md) byl tedy tiše porušený už dřív; dokumentace byla zastaralá, prefab je
autoritativní ([rozhodnutí #14](decisions.md)).

Sweep to řeší **při jakékoli rychlosti** a jako vedlejší efekt vrací **skutečnou normálu povrchu** —
kužel jisker se tak odráží od země správně, místo aby se odhadoval ze směru letu.

**Proč (částice ve world space):** Předchozí verze kreslila `LineRenderer` oblouky s
`LineAlignment.View` v boxu 1,1 m před objektivem. Bylo to zarovnané na obrazovku, přilepené ke
kameře a bez paralaxy — četlo se to jako HUD overlay, ne jako děj ve světě. **World space je ta
podstatná část:** částice zůstanou tam, kde vznikly, takže při letu proplouvají kolem objektivu a
mají paralaxu. Přesně to odlišuje efekt ve světě od překryvu přes obrazovku.

Jaggedness dělá **noise modul**, ne ručně skládané lomené čáry, a viditelný „oblouk" je **trail** za
částicí. To je způsob, jakým se blesk v particle systému dělá.

**Proč (HDR na materiálu):** `ParticleSystem` zapisuje barvu do vertex streamu jako `Color32`, takže
`main.startColor = (5, 0.6, 0.15)` dorazí na GPU jako `(1, 0.6, 0.15)`. Nikdy nepřekročí threshold
Bloomu (1), takže efekt **nezáří** — je to matná šmouha. `LineRenderer.startColor` ořezává stejně,
takže původní oblouky by nezářily taky. `_BaseColor` je skutečný `float4` uniform a HDR přežije;
posílá se přes `MaterialPropertyBlock` (`FxAssets.Tint`), takže se nealokuje materiál navíc a bez
indexu to pokryje i trail materiál.

**Vylučuje:**

- Návrat rakety hráče k diskrétnímu triggeru, nebo „opravu" snížením `speed` zpátky pod 49.
  `OnTriggerEnter` zůstává jen jako záloha za `consumed` guardem.
- HDR barvu v `main.startColor`, `LineRenderer.startColor` / `endColor` nebo v `colorOverLifetime`.
  Ty všechny jedou přes `Color32`. Gradienty tam smí řídit **jen alfu**.
- `LineRenderer` oblouky, `LineAlignment.View` efekty a cokoli zarovnaného na obrazovku nebo
  přilepeného ke kameře — to je ta „UI" varianta, kterou tohle nahrazuje.
- `simulationSpace = Local` u blesků. Zrušilo by to paralaxu, tedy celý smysl.
- Burst na `t = 0` u efektů stavěných v kódu. `AddComponent<ParticleSystem>()` systém rovnou rozjede,
  takže se na hodiny v nule spolehnout nedá — emituje se explicitně přes `Play()` + `Emit(n)`.
- Zrušení červeného rámu HUD. [Rozhodnutí #12](decisions.md) platí dál: rám říká **stav**, částice
  říkají **příčinu**.

**Co to nemění:** nepřátelská raketa (`enemy_rocket.prefab`, `speed: 1080`) prolétá dál a je to
záměr ([rozhodnutí #14](decisions.md)). Sweep by se jí dal dát taky, ale jen kdyby se někdy chtěly
spolehlivé zásahy — což by změnilo ladění boje.

**Kde:** [design/weapons.md](design/weapons.md), [reference/unity-gotchas.md](reference/unity-gotchas.md),
[backlog.md](backlog.md), [../CLAUDE.md](../CLAUDE.md)

---

## #18 — Předělání: arénový survival po vlnách s kartami upgradů

**Rozhodnutí:** Hra se předělává z jedné scény s jedním turretem na **vlny nepřátel ve velkém
uzavřeném boxu**. Každá desátá vlna je boss. Po každé vlně se hra zastaví a nabídne 5 náhodných
karet upgradů s raritou (common 60 % → mythic 1 %). Přibývá main menu (Continue / New game /
Sandbox / Upgrades), ESC pauza a čtyři barevné typy turretů podle projektilu (šedý stun, modrý
electric, červený explozivní, zelený homing). Zdroj: Viktorovy podklady *Základní info o hře* a
*Wavky*, 03.10.2026.

**Proč:** Odpovídá na otevřenou otázku #1 v [concept.md](concept.md) („kam to má směřovat?").
Vlny dávají hře cíl a postup, který dnes chybí, a karty dávají důvod hrát znovu.

**Vylučuje:**

- Otevřenou misi s definovaným cílem a časovku jako hlavní režim. Volný svět zůstává jen jako
  **sandbox** na zkoušení upgradů.
- Jednu scénu jako celou hru — main menu, aréna a sandbox jsou samostatné celky.

**Co to nemění:** pilíře z [concept.md](concept.md) platí — acro let, fyzické projektily
([#3](decisions.md)), turrety s HP ([#5](decisions.md)).

**Napětí s dřívějšími rozhodnutími — nerozhodnuto `[OTEVŘENÉ]`:**

- **[#10](decisions.md)** — pád dnes znovu načte scénu, což by
  v runu smazalo vlnu i karty, a v uzavřeném boxu by zabíjely stěny. Co je smrt v runu, je
  otázka #1 v [design/game-structure.md](design/game-structure.md).
- **[#13](decisions.md)** — boss má health bar **v HUDu**
  nahoře na obrazovce, ne ve světě. Výjimka jen pro bosse; běžné turrety mají pruh dál nad sebou.

**Kde:** [concept.md](concept.md), [design/game-structure.md](design/game-structure.md),
[design/waves.md](design/waves.md), [design/enemies.md](design/enemies.md)

---

## #19 — Obtížnost určuje, co je smrt v runu

**Rozhodnutí:** Při zakládání nové hry se volí obtížnost:

- **Easy** — smrt **znovu načte aktuální vlnu**. Run i karty z předchozích vln zůstávají.
- **Normal** — smrt **ukončí run**.

Co je smrt, se nemění: dron nemá HP a umírá dotekem pevného objektu ([rozhodnutí #10](decisions.md)).
Mění se jen to, **co po ní následuje**.

**Proč:** Odpovídá na napětí s #10 zapsané u [rozhodnutí #18](decisions.md) — restart celé scény
by v runu smazal vlnu i vybrané karty. Easy dává prostor učit se vlnu znovu, Normal drží
roguelite cenu chyby.

**Vylučuje:**

- Restart celé scény jako následek smrti v runu. `SceneManager.LoadScene` z #10 platí dál jen
  mimo run; v runu se resetuje vlna a stav runu žije mimo scénu.
- HP, životy a checkpointy uvnitř vlny — #10 platí, jedna chyba stojí vlnu (Easy) nebo run (Normal).

**Kde:** [design/game-structure.md](design/game-structure.md)
