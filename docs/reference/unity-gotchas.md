# Unity pasti, na které jsme narazili

Věci, které nejsou herní design, ale snadno se ztratí a stály čas.

## Rotace child objektu pod nerovnoměrně škálovaným parentem deformuje mesh

**Projev:** hlaveň turretu se při otáčení vizuálně kroutila (skew), místo aby se jen otáčela.

**Příčina:** `Turret_Base` byl zploštělý válec se scale `(1.5, 0.3, 1.5)`. Když je objekt child
takhle nerovnoměrně škálovaného parenta, jeho rotace se skládá s tím scale a mesh se zkosí. Unity
tuhle kombinaci neumí reprezentovat čistě.

**Řešení:** `Turret_Base` a `Turret_Barrel` musí být **sourozenci** pod společným prázdným parentem
(`turret`), ne parent-child mezi sebou. Skript `EnemyTurret` sedí na tom prázdném parentovi, ne na
`Turret_Base`.

**Obecné pravidlo:** nikdy nedávat rotující objekt jako child objektu s nerovnoměrným scale. Prázdný
parent (scale 1,1,1) je vždy bezpečný kontejner.

## Skript na parentovi, collider na childu → `GetComponent` nestačí

Důsledek předchozí restrukturalizace: `EnemyTurret` je na `turret`, ale collidery jsou na
`Turret_Base` a `Turret_Barrel`. Kolize tedy hlásí *child* objekt, ne ten se skriptem.

Proto `RocketProjectile.OnTriggerEnter` používá `GetComponentInParent<EnemyTurret>()`, ne
`GetComponent`. Se `GetComponent` by zásahy tiše nic nedělaly. `[OVĚŘENO 03.09.2026]` — v kódu je
to správně.

## Kinematický Rigidbody ignoruje Continuous Collision Detection

Tohle byla příčina bugu s prolétáváním raket. **Opraveno** — viz
[rozhodnutí #8](../decisions.md). Past sama platí dál a je potřeba ji znát.

Unity **neaplikuje CCD na kinematické Rigidbody** — `collisionDetectionMode = Continuous` na
kinematickém tělese je no-op. Nastavit CCD a nechat `isKinematic = true` tedy problém nevyřeší,
i když to v Inspectoru vypadá správně.

Navíc: kinematické těleso negeneruje `OnCollisionEnter` proti **statickým** colliderům (těm bez
Rigidbody) — jen proti nekinematickým Rigidbody. A všechny collidery turretu statické jsou, na
`turret`, `Turret_Base` ani `Turret_Barrel` není žádné Rigidbody.

**Protějšek, na kterém stojí oprava:** kinematické těleso s colliderem na **`isTrigger`** proti
statickým colliderům trigger zprávy **generuje** (Unity collision action matrix, řádek *Static
Collider × Kinematic Rigidbody Trigger Collider* = Yes). Detekce zásahu tedy jde přes
`OnTriggerEnter` a fyzikální reakce zůstane nulová.

**Druhá polovina:** trigger test je diskrétní, takže spolehlivost stojí na tom, že posun projektilu
za jeden fyzikální krok je **menší než délka jeho collideru** — testované pozice se pak v prostoru
překrývají a nevznikne mezera. Proto je pohyb v `FixedUpdate` (`speed × 0,02` = 0,6 m u rakety
hráče vs. collider 0,986 m), ne v `Update()`, kde by ta vzdálenost závisela na frameratu.

## Tagy jsou tichý kontrakt

Tagy nesou gameplay kontrakt a **nic nespadne**, když chybí:

- `Player` → na objektu `Drone`. Čte ho `EnemyTurret` (míření) a `EnemyProjectile` (komu dát damage).
  Když zmizí, turret přestane hráče vidět. Bez jediné chybové zprávy.
- `Enemy` → na `Turret_Base` a `Turret_Barrel`. **Od [rozhodnutí #8](../decisions.md) ho nečte už
  žádný kód** — zůstává jako sémantické značení. Nemazat, ale nespoléhat na něj.

`Player` je Unity built-in tag; `Enemy` je jediný vlastní tag deklarovaný v
`ProjectSettings/TagManager.asset`.

**Historická poznámka:** self-collision projektilů se dřív řešila `Physics.IgnoreCollision` nad
objekty nalezenými podle tagu. Tichý problém byl, že objekt `turret` je `Untagged`, ale má vlastní
CapsuleCollider a `TurretFirePoint` na něm nepřátelskou raketu spawnuje — smyčka podle tagu `Enemy`
ho tedy nikdy nepokryla. Teď se střelec předává explicitně jako `owner` při `Instantiate` a filtruje
se `other.transform.IsChildOf(owner)`, což pokryje celou hierarchii včetně netagovaného rootu.

## `Assets/_Recovery/0.unity`

Crash-recovery snapshot od Unity, který se omylem dostal do initial commitu. Není součástí buildu
a **není to pracovní scéna** — pracovní je `Assets/Scenes/SampleScene.unity`. Kdo ji omylem otevře,
edituje slepou kopii.
