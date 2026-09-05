# Backlog

Otevřené bugy a nápady. U každého je uvedený **ověřený stav** k 03.09.2026 — poznámky ze session
některé věci označovaly jako otevřené, které už jsou hotové, a naopak.

---

## Bugy

### ~~1. Rakety hráče prolétávají skrz turret bez kolize~~

**Stav: OPRAVENO** `[OVĚŘENO 03.09.2026]`. Zvolena varianta „trigger na kinematickém tělese" —
zdůvodnění a co to vylučuje je v [rozhodnutí #8](decisions.md).

Root cause byl, že kinematické Rigidbody negeneruje `OnCollisionEnter` proti statickým colliderům
turretu, a `Continuous` CCD na `rocket.prefab` je na kinematickém tělese no-op. Oprava v obou
projektilech: collider na `isTrigger`, `OnCollisionEnter` → `OnTriggerEnter`, pohyb přes
`rb.MovePosition` ve `FixedUpdate` (frameratová nezávislost) a self-hit filtr přes explicitní
`owner` místo `Physics.IgnoreCollision` podle tagu.

Vedlejší efekty, které z toho vypadly a jsou žádoucí: projektily teď mizí při zásahu terénu místo
aby jím prolétly do `lifeTime`, a `EnemyProjectile` už při každém výstřelu nevolá
`FindGameObjectsWithTag`. Zbývá kosmetický úklid — prefaby v Inspectoru dál ukazují
`Is Kinematic` a `Continuous`, autoritativní je kód ve `Start()`.

### ~~2. Turret míří „nad hráče"~~

**Stav: OPRAVENO** `[OVĚŘENO 04.09.2026]`. Zdůvodnění a co to vylučuje je
v [rozhodnutí #9](decisions.md).

**Hypotéza „vadí pravděpodobně jen vizuál" neplatila.** Byly to dvě vady se stejnou příčinou:

- **Vizuální:** mesh kapsle má dlouhou osu Y, `LookRotation` zarovnává +Z → hlaveň mířila bokem.
- **Funkční, doteď nezaznamenané:** směr se počítal z `barrel.position` (pivot), ale projektil
  spawnoval na `firePoint.position` = `(0, 0.64, 0.568)` × scale hlavně 2, tedy **1,28 m mimo osu
  míření**. Střely letěly souběžně vedle hráče, konstantně na jakoukoli vzdálenost. Dron má collider
  vysoký 0,387 m → nepřátelské projektily ho **nemohly zasáhnout nikdy**.

Ten `y: 0.64` tam byl proto, že někdo dal ústí na viditelný hrot té svislé kapsle — jedna neshoda
konvence, dva symptomy.

Oprava: v kódu se míří z `firePoint.position`, ve scéně se mesh odstrčil do childa
`Turret_BarrelMesh` (rotace 90° X), `TurretFirePoint` se posunul na osu +Z a z `turret` rootu se
smazal `CapsuleCollider` — po srovnání hlavně by z něj zůstal 4,5 m vysoký neviditelný sloup nad
základnou, do kterého by se registrovaly zásahy do prázdna.

### ~~3. Dron nedostává žádný damage~~

**Stav: VYŘEŠENO** `[OVĚŘENO 04.09.2026]` — zrušením damage modelu, ne jeho dodělením. Zdůvodnění
a co to vylučuje je v [rozhodnutí #10](decisions.md).

Root cause byl, že komponenta `DroneHealth` nebyla ve scéně na žádném objektu:
`EnemyProjectile.OnTriggerEnter` sice trefil objekt s tagem `Player`, ale
`GetComponent<DroneHealth>()` vrátil `null` a kód tiše neudělal nic.

Blokovala to otevřená otázka #3 v [concept.md](concept.md) — dokud nebylo rozhodnuté, co se má stát
při smrti hráče, nešlo komponentu jen tak přidat. Odpověď: **dron nemá HP vůbec.** `DroneHealth` je
smazaná, jakýkoli dotek pevného objektu restartuje scénu a nepřátelská raketa místo damage bere na
`stunDuration` sekund ovládání.

### ~~4. Turret vystřelí okamžitě při návratu hráče do dosahu~~

**Stav: NENÍ BUG — ZÁMĚR** `[ROZHODNUTO 05.09.2026]`, viz [rozhodnutí #15](decisions.md).

Popis platí a kód se nemění: `EnemyTurret.Update()` se při hráči mimo `detectionRange` ukončí dřív,
než dojde na kontrolu `nextFireTime`, cooldown ale běží dál na reálném čase — návrat do dosahu po
víc než `fireRate` sekundách znamená ránu okamžitě, bez náběhu. Po hraní je to **žádoucí**: návrat
do dosahu je hráčovo rozhodnutí a má mít cenu. Reset `nextFireTime` při ztrátě cíle i jakékoli
telegrafování první rány jsou tím vyloučené.

---

## Technický dluh

### ~~`fireRate` ve scéně nesouhlasí s docs~~ — vyřešeno

**Stav:** uzavřeno `[05.09.2026]`, viz [rozhodnutí #14](decisions.md).

Scéna měla `fireRate: 0.1` proti 2 s v docs. Vyhrála **scéna**: po hraní je to `0.2` (5 ran/s)
a dokumentace se srovnala s ní, ne naopak. `stunImmunity` klesla z 2 s na 0,5 s, protože i-frames
už nemusí krýt 10 zásahů za sekundu.

Zůstává v platnosti obecná past: defaulty v `EnemyTurret.cs` (30 / 2 s / 90 / 30) **nejsou** to, co
se hraje. Nový turret přetažený do scény se musí nastavit ručně.

### Nepřátelská raketa prolétává cílem (vědomě)

**Stav:** aktivní, **není to bug k opravě** `[OVĚŘENO 05.09.2026]`.

`enemy_rocket.prefab` má `speed: 1080` → 21,6 m za fyzikální krok proti collideru 0,986 m. Zásah se
proto registruje jen náhodou, zhruba v 6 % přímých letů. Je to vědomá cena za pocit ze hry
([rozhodnutí #14](decisions.md)), zapsaná sem, aby ji příště někdo neopravoval jako regresi.

Co by to změnilo na navrženou vzácnost místo náhodné: sweep test (`Physics.SphereCast` po dráze mezi
dvěma kroky) místo diskrétního `OnTriggerEnter`, plus zpátky nižší `speed`, kdyby se ukázalo, že
spolehlivý zásah je moc. Obojí znamená znovu otevřít [rozhodnutí #8](decisions.md). `[OTEVŘENÉ]`

### URP particle shader není v Always Included Shaders

`FxAssets` shání materiál efektů přes
`Shader.Find("Universal Render Pipeline/Particles/Unlit")`. `Shader.Find` ale vidí jen shadery, na
které se v buildu někdo odkazuje, a v tomhle projektu se neodkazuje nikdo —
`ProjectSettings/GraphicsSettings.asset` má v `m_AlwaysIncludedShaders` jen 7 builtin shaderů.

V editoru to funguje vždycky. **V player buildu vrátí `null`**, `FxAssets` zaloguje warning a
spadne zpět na `Sprites/Default` — efekty se vykreslí, ale nebudou aditivní.

Až se bude poprvé dělat build: *Edit > Project Settings > Graphics > Shader Loading > Always
Included Shaders*, přidat `Universal Render Pipeline/Particles/Unlit`. Přes UI, ne ruční editací
YAML. Viz [rozhodnutí #16](decisions.md). `[OTEVŘENÉ]`

### Slupka výbojů je naladěná na současnou kameru

`DroneStunArcs` emituje v kulové slupce `fieldCenter (0, 0, 0.3)` / `fieldRadius 1.8` /
`fieldThickness 0.45`. Je to naladěné tak, aby nejbližší částice minuly near clip 0,3 m kamery, která
sedí na drone-local `(0, 0.12, 0.629)` — trup je celý za její rovinou, takže výboje na něm by nebylo
vidět ([rozhodnutí #16](decisions.md) a [#17](decisions.md)).

**Jakmile se kamera hne** — nebo přibude chase pohled — je potřeba slupku přeladit. U chase kamery ji
lze zmenšit a posadit na trup. Obě gizma (vnější i vnitřní poloměr) se kreslí při vybraném dronu ve
Scene view. `[OTEVŘENÉ]`

### Dokumentace tvrdila `speed: 30`, prefab má 120 `[VYŘEŠENO 05.09.2026]`

`rocket.prefab` má `speed: 120`, ale `CLAUDE.md` i [design/weapons.md](design/weapons.md) roky
tvrdily 30 a odvozovaly z toho, že raketa hráče je „hluboko pod stropem ~49". Nebyla — prolétala
zhruba 59 % zásahů. Opraveno v obou dokumentech, detekce převedena na sweep test
([rozhodnutí #17](decisions.md)).

Poučení, ne úkol: **prefab je autoritativní, dokumentace ne** ([rozhodnutí #14](decisions.md)). Když
se v docs objeví konkrétní číslo z prefabu nebo scény, patří k němu ověření, ne důvěra.

### Damage rakety hráče je hardcoded

`RocketProjectile.OnTriggerEnter` volá `turret.TakeDamage(10)` — číslo je zadrátované v místě
volání. `[AKTUALIZOVÁNO 04.09.2026]` Protějšek `EnemyProjectile.damage` už neexistuje (dron nemá HP,
viz [rozhodnutí #10](decisions.md)), takže je to jediná zbývající zadrátovaná hodnota poškození —
asymetrie zmizela, tenhle dluh zůstal.

### Duplikovaná detekce vysílačky

`DroneControls.Start()` a `Shoting.Start()` mají každý vlastní kopii hledání zařízení. Viz otevřená
otázka v [design/controls.md](design/controls.md).

### `Assets/_Recovery/0.unity` v repu

Crash-recovery snapshot, který se dostal do initial commitu. Smazat, nebo nechat?

### Materiály `Laser.mat` a `enemy laser.mat`

V projektu jsou lasery, ale zbraně jsou rakety. Zbytek staršího nápadu? Viz
[design/weapons.md](design/weapons.md).

---

## Nápady

Nezadané, jen zaparkované. Rozpracované varianty jsou v sekcích `[OTEVŘENÉ]` u jednotlivých
design dokumentů:

- Line-of-sight pro turrety → terén jako kryt ([design/enemies.md](design/enemies.md))
- Letecký nepřítel — souboj dron vs. dron
- Zpětný ráz při výstřelu ([design/weapons.md](design/weapons.md))
- Expo/rate křivky sticků ([design/flight-model.md](design/flight-model.md))
