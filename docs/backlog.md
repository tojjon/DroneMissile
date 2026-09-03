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

### 2. Turret míří „nad hráče"

**Stav:** aktivní. **Silná hypotéza** `[HYPOTÉZA]`, podložená ověřenými fakty.

Poznámky ze session to nechávaly nediagnostikované s tím, že to bude souviset s `barrel.position` po
restrukturalizaci hierarchie. Zjištěná fakta ukazují jinam:

- `Turret_Barrel` je Unity **Capsule** primitiv (mesh `10208`) → jeho **dlouhá osa je Y**.
- `EnemyTurret.Update()` míří přes `Quaternion.LookRotation(direction)`, což zarovnává **+Z**
  (forward) na cíl.

Kapsle tedy míří na hráče *bokem* — její délka zůstane na směr míření kolmá, což vypadá přesně jako
„hlaveň trčí vzhůru". Není to chyba výpočtu, je to neshoda mezi osou meshe (Y) a osou, kterou
`LookRotation` zarovnává (Z).

Ověřené vylučovací body: `Turret_Barrel` má `localScale (2,2,2)` — **uniformní**, takže skew
z [rozhodnutí #4](decisions.md) to není, a `localRotation` je identita.

**Fix k ověření:** obalit kapsli prázdným objektem otočeným o 90° kolem X (mesh pak míří po Z), nebo
mířit `barrel.rotation = LookRotation(direction) * Quaternion.Euler(90, 0, 0)`. Pozor, že
`TurretFirePoint` je posunutý po **Z** (`+0.568`) — směr střelby tedy už s +Z jako „vpřed" počítá,
takže vadí pravděpodobně jen vizuál. Než se to opraví, je potřeba potvrdit, kam projektily reálně
letí.

### 3. Dron nedostává žádný damage

**Stav:** aktivní, **dosud nezaznamenané** `[OVĚŘENO 03.09.2026]`.

**Komponenta `DroneHealth` není ve scéně na žádném objektu.** `EnemyProjectile.OnCollisionEnter`
sice trefí objekt s tagem `Player`, ale `GetComponent<DroneHealth>()` vrátí `null` a kód tiše
neudělá nic. Nepřátelské projektily jsou aktuálně **naprosto neškodné**.

Souvisí s otevřenou otázkou v [concept.md](concept.md): dokud není rozhodnuté, co se má stát při
smrti hráče, nemá smysl komponentu jen tak přidat.

### 4. Turret vystřelí okamžitě při návratu hráče do dosahu

**Stav:** aktivní, drobnost `[OVĚŘENO 03.09.2026]`.

`EnemyTurret.Update()` se při hráči mimo `detectionRange` ukončí dřív, než dojde na kontrolu
`nextFireTime` — cooldown ale běží dál na reálném čase. Hráč, který odletí a vrátí se po víc než
`fireRate` sekundách, dostane výstřel okamžitě, bez náběhu.

---

## Technický dluh

### Damage rakety hráče je hardcoded

`RocketProjectile.OnTriggerEnter` volá `turret.TakeDamage(10)` — číslo je zadrátované v místě
volání, zatímco `EnemyProjectile` má `damage` jako pole v Inspectoru. Asymetrie bez důvodu.

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
- Crash damage při nárazu do terénu
