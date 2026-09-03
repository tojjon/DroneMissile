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

Proto `RocketProjectile.OnCollisionEnter` používá `GetComponentInParent<EnemyTurret>()`, ne
`GetComponent`. Se `GetComponent` by zásahy tiše nic nedělaly. `[OVĚŘENO 03.09.2026]` — v kódu je
to správně.

## Kinematický Rigidbody ignoruje Continuous Collision Detection

Tohle je aktivní příčina bugu s prolétáváním raket, viz [backlog.md](../backlog.md).

Unity **neaplikuje CCD na kinematické Rigidbody** — `collisionDetectionMode = Continuous` na
kinematickém tělese je no-op. Nastavit CCD a nechat `isKinematic = true` tedy problém nevyřeší,
i když to v Inspectoru vypadá správně.

Navíc: kinematické těleso negeneruje `OnCollisionEnter` proti **statickým** colliderům (těm bez
Rigidbody) — jen proti nekinematickým Rigidbody.

## Tagy jsou tichý kontrakt

Celá collision-ignore logika stojí na tazích a **nic nespadne**, když chybí:

- `Player` → na objektu `Drone`, aby rakety hráče ignorovaly kolizi s vlastním dronem.
- `Enemy` → na `Turret_Base` a `Turret_Barrel`, aby nepřátelské projektily ignorovaly kolizi
  s vlastním turretem.

Když tag zmizí, `Physics.IgnoreCollision` se nenastaví a projektil vybuchne hned po vypuštění o svého
vlastního střelce. Bez jediné chybové zprávy.

`Player` je Unity built-in tag; `Enemy` je jediný vlastní tag deklarovaný v
`ProjectSettings/TagManager.asset`.

## `Assets/_Recovery/0.unity`

Crash-recovery snapshot od Unity, který se omylem dostal do initial commitu. Není součástí buildu
a **není to pracovní scéna** — pracovní je `Assets/Scenes/SampleScene.unity`. Kdo ji omylem otevře,
edituje slepou kopii.
