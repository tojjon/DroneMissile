# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Unity 3D game: a player-piloted drone fights ground turrets over terrain. Flight input comes from a
physical RC transmitter (exposed as a HID joystick) with a keyboard fallback.

- Unity **6000.5.10f1** (pinned in `ProjectSettings/ProjectVersion.txt`; the editor must match exactly)
- **URP** (Universal Render Pipeline) 17.5.0 — PC and Mobile render pipeline assets under `Assets/Settings/`
- **Input System** 1.20.0 (the new package, not legacy `Input`) — `Assets/InputSystem_Actions.inputactions`
- Four scenes in the build, in this order: `MainMenu` (index 0 — the game launches into it),
  `Arena` (the wave run — New game loads it), `SampleScene` (open-terrain free flight), `Sandbox` —
  all in `Assets/Scenes/`
- Installed editor path: `C:/Program Files/Unity/Hub/Editor/6000.5.10f1/` on Windows,
  `~/Unity/Hub/Editor/6000.5.10f1/` on Viktor's Linux machine (see the Flatpak note under Commands)

## Design intent

`docs/` holds the **why** — game concept, design requirements, hardware findings and the
decision log. This file holds the **how**. When the two disagree, `docs/` describes the intent
and this file describes the current implementation; treat the gap as work to do, not as a doc bug.

Read before changing gameplay behaviour:

- [docs/README.md](docs/README.md) — index and conventions
- [docs/decisions.md](docs/decisions.md) — **decisions and what they rule out.** Several
  things that look like cleanup opportunities are deliberate (thrust along local up with no
  separate move force, the sibling turret hierarchy, the device-enumeration logging in
  `DroneControls.Start()`). Check here before "fixing" them.
- [docs/reference/radiomaster-pocket.md](docs/reference/radiomaster-pocket.md) — the transmitter
  control paths and axis ranges. Empirically discovered, not guessable, and wrong values fail
  silently.
- [docs/backlog.md](docs/backlog.md) — open bugs with verified root causes.

**New features go through `docs/plans/` — always, before any code.** The full lifecycle is in
[docs/plans/README.md](docs/plans/README.md); in short:

1. When Viktor asks for a new feature, first create `docs/plans/<feature>.md` (status *plánováno*:
   the requirement, what already exists, open questions) and add it to the list in
   `docs/plans/README.md`.
2. Write the implementation plan into that same document (section *Plán implementace*) — when a
   plan is made in plan mode, this is where it ends up, translated into Czech like the rest of
   `docs/`. Status → *rozpracováno*.
3. While building, record deviations from the plan in the document (*Odchylky při implementaci*).
4. When the feature is done: describe it in `docs/features/` (one feature = one document, plus its
   row in `docs/features/README.md`), log decisions in `docs/decisions.md`, then **move the plan to
   `docs/plans/archive/`** with status *hotovo* and the commit hash, add it to
   `docs/plans/archive/README.md`, and remove it from the active list.

`docs/` is written in Czech; follow the markers in [docs/README.md](docs/README.md).

## Commands

There is no build script, CI, or task runner in this repo. Builds are made from the editor
(File > Build Profiles). The Unity CLI is the only way to compile or test headlessly:

```bash
UNITY="/c/Program Files/Unity/Hub/Editor/6000.5.10f1/Editor/Unity.exe"

# Compile check — imports assets, compiles scripts, exits. Read the log for CS errors.
"$UNITY" -batchmode -quit -projectPath . -logFile - | grep -E "error CS|Compilation failed"

# Run tests (see "Tests" below — no test assembly exists yet)
"$UNITY" -runTests -batchmode -projectPath . -testPlatform EditMode -testResults results.xml
"$UNITY" -runTests -batchmode -projectPath . -testPlatform PlayMode -testResults results.xml

# Single test / subset — filter is a regex over the full test name
"$UNITY" -runTests -batchmode -projectPath . -testPlatform EditMode \
         -testFilter "EnemyTurretTests.TakeDamage_ReducesHealth" -testResults results.xml

# Run an editor builder headlessly (the Tools > DroneMissile menu items)
"$UNITY" -batchmode -quit -projectPath . -executeMethod MainMenuBuilder.BuildAll -logFile -
```

**On Viktor's Linux machine both Unity Hub and VS Code are Flatpaks**, and the Personal license
lives inside the Hub's sandbox. Running the editor binary directly fails with *"No valid Unity
Editor license found"*; run it inside the Hub sandbox instead, and write logs under `$HOME` — each
sandbox has its own `/tmp`:

```bash
flatpak-spawn --host flatpak run \
  --command=$HOME/Unity/Hub/Editor/6000.5.10f1/Editor/Unity com.unity.UnityHub \
  -batchmode -quit -nographics -projectPath "$PWD" -logFile ~/.cache/drone-compile.log
```

**Only one process may hold the project at a time.** Unity locks `Library/`, so every CLI command
above fails while the project is open in the editor. Close the editor first, or expect
`Multiple Unity instances cannot open the same project`.

Test results land in `results.xml` (NUnit format) — parse that, not stdout. `-logFile -` sends the
editor log to stdout; without it the log goes to `Logs/` (gitignored).

## Architecture

Twenty-two `MonoBehaviour` scripts plus three static classes (`FxAssets`, `GameSession`, `UiKit`) in `Assets/scripts/`, no assembly
definitions — everything compiles into the default `Assembly-CSharp`. There is no manager, service
locator, or event bus; components find each other at runtime through **Unity tags**, and are wired
to prefabs/scene objects through serialized public fields set in the Inspector. `Assets/Editor/`
holds editor-only menu items that build scene objects (`Tools > DroneMissile > ...`) — that is how
scene geometry, the HUD object, the turret's health-bar component and the drone's stun-arc component
get into the scene here, since scene YAML is never hand-edited.

**Tag contract** (breaking these silently disables gameplay — nothing throws):

| Tag | On | Read by |
|---|---|---|
| `Player` | the drone root | `EnemyTurret` (targeting), `EnemyProjectile` (stun on hit), `DroneHUD` (finds the drone) |
| `Enemy` | turret objects | **nothing — no longer read by any code** |

`Player` is a Unity built-in tag; `Enemy` is the only custom tag declared in
`ProjectSettings/TagManager.asset`. Self-collision used to be tag-driven; since
[decision #8](docs/decisions.md) each projectile gets an explicit `owner` transform instead, so
`Enemy` survives only as semantic labelling. Leave it in place, but don't wire new logic to it.

**Combat flow.** Both directions are symmetric but do *not* share code:

- Player: `Shoting` instantiates `rocket.prefab` (`RocketProjectile`) → a **raycast sweep** in
  `FixedUpdate` (not the trigger — see the projectile section) walks
  `GetComponentInParent<EnemyTurret>()` and calls `TakeDamage(10)` — damage is hardcoded at the
  call site, not a field on the projectile.
- Enemy: `EnemyTurret` tracks the player within `detectionRange`, rotates `barrel` toward it, and
  instantiates `enemy_rocket.prefab` (`EnemyProjectile`) → on trigger calls
  `DroneControls.Stun(stunDuration)`. The drone has no health, so a hit takes control away instead
  of dealing damage — no thrust and no `MoveRotation` for a second, so it holds its attitude and
  falls. `stunDuration` *is* a projectile field; the i-frames that stop a 5 shot/s turret from
  pinning the drone (`stunImmunity`, 0.5 s in the scene) live on `DroneControls`.

  **Scene values override every script default here, and by a lot** ([decision
  #14](docs/decisions.md)): the scene turret runs `detectionRange: 360`, `fireRate: 0.2`,
  `turnSpeed: 720`, `maxHealth: 100` (so ten player hits, not three), against script defaults of
  30 / 2 / 90 / 30. Read the scene, not `EnemyTurret.cs`, when reasoning about how the game plays.

Both spawners set the projectile's `owner` field to their own `transform` right after `Instantiate`.
That is the self-hit filter — both projectiles spawn partly inside their shooter's collider, so a
missing `owner` means the projectile detonates on its muzzle. It is logged as a warning, not silent.

**`+Z` is forward everywhere, and the turret aims from the muzzle.** `EnemyTurret.Update()` computes
`direction` from **`firePoint.position`**, not `barrel.position`: the muzzle sits off the pivot axis,
so aiming from the pivot sends every shot flying parallel to the player, missing by the muzzle offset
at any range. `Fire()` then spawns along `firePoint.rotation`, which equals `barrel.rotation` only
because `TurretFirePoint` has an identity local rotation — keep it that way. Meshes that don't match
the convention (Unity's `Capsule`/`Cylinder` are long along **Y**) belong in a rotated child, never in
a compensating rotation applied to the aim transform. Both constraints are
[decision #9](docs/decisions.md).

**The drone has no health — any solid contact ends the run.** `DroneControls.OnCollisionEnter` calls
`Crash()` on contact with anything: terrain, the ground slab, a turret. Landing counts. `DroneHealth`
is deleted; there is no health, respawn, or checkpoint, and a reload restores the turret's HP too.

`Crash()` does **not** reload on the spot ([decision #12](docs/decisions.md)): it sets `hasCrashed`
and a `reloadAt = Time.time + deathDelay` deadline, and `FixedUpdate` calls
`SceneManager.LoadScene(GetActiveScene().buildIndex)` once that passes. Physics keeps stepping for
that second, so the wreck tumbles under `DroneHUD`'s death message. Nothing can re-enter the path —
`OnCollisionEnter` tests `!hasCrashed`, `Stun()` bails on it, and the `killY` check sits *below* the
`hasCrashed` return in `FixedUpdate`. `Time.timeScale` is deliberately left alone.

Three guards in `DroneControls` look like cruft and are not — all three are
[decision #10](docs/decisions.md):

- `armAltitude` — a one-way latch. The drone spawns *resting on the pad*, so without it the very
  first contact fires at t=0 and the scene reloads forever. Crashes only count once the drone has
  climbed `armAltitude` above its spawn height.
- `killY` — a kill plane, deliberately **not** gated on that latch. The slab is a floating platform
  with terrain 57 m below; sliding off the edge is a fall the collision path can miss, and nothing
  in the project can restart a run.
- `stunImmunity` — i-frames, 0.5 s in the scene. At the scene's `fireRate: 0.2` a refreshing 1 s
  stun would leave the drone limp until it hit the ground. (It was 2 s back when `fireRate` was
  `0.1`; [decision #14](docs/decisions.md) halved the fire rate and cut the i-frames to match.)

The drone also runs `ContinuousDynamic` CCD and `Interpolate`, set from code in `Start()`
([decision #11](docs/decisions.md)). Decision #8's "CCD is a no-op" applies to the *kinematic
projectiles* — the drone is dynamic, and with `Discrete` its 0.387 m collider falls straight through
the terrain heightfield above ~19.4 m/s, which one drop off the pad exceeds.

Only `EnemyTurret` dies in the ordinary sense (`Destroy(gameObject)`). There is no win condition.

**Four turret types share `EnemyTurret` and differ by projectile prefab** ([decision
#22](docs/decisions.md)). `EnemyProjectile` is now a base class and on its own is the **grey**
turret's shot. Since [decision #24](docs/decisions.md) the grey turret fires like the others —
every 2 s, 60 m/s stones with the reliable sweep; only the original `enemy_rocket.prefab` on
SampleScene's own turret still tunnels at 1080 m/s (#14). `ElectricProjectile` (blue; on impact a particle `ElectricZap` jumps to
the drone within `zapRange`), `ExplosiveProjectile` (red; `OverlapSphereNonAlloc` stuns within
`blastRadius`) and `HomingProjectile` (green; `Steer()` rotates toward the drone at `turnRate`) set
`useSweep: true` and override the virtual hooks `OnImpact`, `Steer` and `OnExpire`. The sweep is the
same technique as `RocketProjectile.Sweep`, duplicated in the base; both hit paths funnel through one
`consumed`-guarded `Hit()`. Lifetime runs through `Invoke(Expire)`, not `Destroy(gameObject, t)`, so
a subclass can leave an effect behind. `visualPrefab` is the slot for the grey turret's rock model.

`DroneControls.Stun(seconds, StunKind)` records `LastStunKind`, and the drone carries **two
`DroneStunArcs`**, one per kind: `Electric` (the original arcs) and `Rock` (yellow debris: no noise,
no trails, gravity) — each plays only for its own kind. `stunKind` is a field on each projectile
prefab. `MainMenuBuilder` strips all instances.

`Assets/Editor/TurretTypesBuilder.cs` (`Tools > DroneMissile > Build Turret Types`) generates the
materials (`turret_*.mat`, `proj_*.mat`), the projectile prefabs (`Assets/3D models/enemy_{rock,
electric,explosive,homing}`) and the turret prefabs (`Assets/Prefabs/Turrets/Turret_*`, saved from a
temporary copy of SampleScene's `turret`), adds the rock stun effect to the drone, and places one of
each type in `Sandbox` **600 m from the pad** — beyond the 360 m detection range, so they engage one
at a time. It **only creates missing assets**: Inspector tuning on the prefabs survives a rerun;
delete an asset to regenerate it. SampleScene keeps its own turret and `enemy_rocket.prefab`.

**Effects are built in code too, and none of them is an asset.** There is no ParticleSystem, shader,
`.mat` or `.png` for VFX anywhere in `Assets/` and VFX Graph is deliberately not installed — all
[decision #16](docs/decisions.md). `FxAssets` is a static class holding the only two materials and
two textures any effect uses, built lazily on first access and shared: a per-hit `Material` would
leak, since runtime-created Objects are not garbage collected. Two things there are load-bearing:

- A code-created `ParticleSystemRenderer` arrives with the **builtin** default particle material,
  which is **magenta under URP**. Every renderer must be handed `FxAssets.AdditiveDot`/`AdditiveBand`
  explicitly, and via `sharedMaterial` — `material` instantiates a private copy per renderer.
- Additive blending is **not** a keyword. `ParticlesUnlit.shader` reads
  `Blend[_SrcBlend][_DstBlend] ZWrite[_ZWrite]` off the material, and URP sets those floats from an
  *editor-only* ShaderGUI that does not exist at runtime. `FxAssets.BuildAdditive()` is that method
  transcribed by hand. `Shader.Find` also returns `null` in a **player build** until the shader is
  added to Project Settings > Graphics > Always Included Shaders — logged in
  [docs/backlog.md](docs/backlog.md), and it warns and falls back rather than going magenta.

**HDR colour cannot go through the particle system.** `main.startColor` and
`LineRenderer.startColor` are written into the vertex stream as `Color32`, so anything above 1 is
clamped on its way to the GPU and can never cross Bloom's threshold of 1 — the effect renders as a
dull smudge instead of a glow, with nothing in the console to say why. Every effect here therefore
sets `startColor = Color.white` and puts the HDR tint on `_BaseColor` through
`FxAssets.Tint(renderer, hdrColor)`, a `MaterialPropertyBlock` (no allocation, nothing to destroy,
and with no index it covers a `ParticleSystemRenderer`'s separate trail material too).

`RocketProjectile.HandleHit` spawns an `ImpactExplosion` on **every** impact — terrain, slab,
turret — before the `TakeDamage` call, so a turret that dies to the hit cannot cost the effect. It
builds in `Start()`, not `Awake()`: `AddComponent` runs `Awake` synchronously *inside the call*, so
the spawner's field assignments on the next line would be missed. (`RocketProjectile.Awake` carries
the opposite note for the opposite reason.) Position and orientation come from the sweep's
`hit.point`/`hit.normal`, so the spark cone fires off the real surface. Never use
`other.ClosestPoint()` here — it is undefined for `TerrainCollider` and non-convex meshes and hands
back the collider's transform origin, i.e. an explosion hundreds of metres away.

Particles are emitted with an explicit `ps.Play(); ps.Emit(n);`, **not** a burst at `t = 0`. A burst
depends on the system clock still being at zero when the first simulation step runs, and it is not —
`AddComponent<ParticleSystem>()` starts the system playing before any configuration lands.

**`DroneStunArcs` is one `ParticleSystem`, simulated in world space, and it is not a child of the
drone.** All three are deliberate ([decisions #16 and #17](docs/decisions.md)):

- **`simulationSpace = World` is the effect**, not an optimisation. Particles are left behind as the
  drone flies, so they stream past the lens and *parallax*. That parallax is the whole difference
  between an effect that lives in the world and one that reads as a HUD overlay — which is exactly
  how the `LineRenderer` version this replaced looked, being screen-aligned and camera-locked.
- The arcs are **noise + trails**, not geometry. A strong high-frequency `noise` module throws each
  particle along an erratic zig-zag and the `trails` module draws the visible filament behind it.
  That is the particle idiom for lightning; hand-built jagged polylines were the wrong tool.
- The rig is an **unparented root object at scale 1** whose pose is copied from the drone in
  `LateUpdate`, because the `Drone` root's scale is `(0.61325, 0.38720787, 1)` and a child would
  inherit that squash — the same trap as [decision #4](docs/decisions.md); particle `startSize` is
  driven by lossy scale. (`TurretHealthBar` *can* be a child; the turret root's scale is 1.)
- Emission is a **hollow shell** (`fieldCenter`/`fieldRadius`/`fieldThickness`) sized so its nearest
  particles clear the camera's 0.3 m near clip. `Main Camera` sits at drone-local `(0, 0.12, 0.629)`
  while the body is a 1 m cube at the origin, so the camera is 0.129 m *in front of the nose* and
  the whole hull is behind the camera plane — anything drawn on the body is invisible in first
  person. `OnDrawGizmosSelected` draws both shell radii.

It polls `DroneControls.IsStunned` like the HUD, which is what makes i-frame-rejected hits show
nothing. Fade-out needs no code: emission stops and the particles in flight finish their own lives.

`ParticleSystem` modules are **struct handles**: `ps.main.startColor = c` is CS1612. Copy the module
into a local (`var main = ps.main;`) — the copy still points at the same native system.

**`AddComponent<ParticleSystem>()` returns a system that is already playing** (`playOnAwake` defaults
on), so both builders call `ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear)` on the
very next line, before touching any module. Skipping it costs an audible error — `main.duration` is
rejected while playing — *and* a silent one: the clock is already past 0, so a burst scheduled at
`t = 0` never fires and the trailing `ps.Play()` is a no-op on an already-playing system. The effect
then builds without further complaint and renders nothing.

`ps.Play()` also does nothing on an inactive GameObject, and `playOnAwake` fires only on the first
`Awake`, not on every re-enable — so `DroneStunArcs` deactivates its rig last and starts the sparks
with an explicit `Play()` in `LateUpdate` when the rig comes back, or every stun after the first
would have no sparks.

`Tools > DroneMissile > Build Drone Stun Arcs` (`Assets/Editor/DroneStunArcsBuilder.cs`) puts the
component on the `Player`-tagged root. The explosion needs no builder — the call site is already in
`RocketProjectile`.

**The turret wears its health on a world-space bar.** `TurretHealthBar` sits on the *same* object as
`EnemyTurret` — the empty `turret` root, which never rotates (a bar on `Turret_Barrel` would swing
around the head) — and builds its own **`RenderMode.WorldSpace`** Canvas child named `HealthBar` in
`Start()`: a dark `Background`, a padded `FillArea`, and a `Fill` whose `anchorMax.x` *is* the health
fraction. World space, not the `DroneHUD` overlay, is [decision #13](docs/decisions.md): the bar
belongs to one turret, so terrain must occlude it and it must shrink with distance. `sizeDelta` is
therefore in **metres** and assumes the `turret` root's scale is 1. `LateUpdate` (not `Update` — the
billboard has to come after anything that moved the camera this frame) polls
`EnemyTurret.HealthFraction`, eases the fill toward it, tints it green→yellow→red (the *scene*
overrides `fullColor` and `midColor` to plain red, so in play the bar only changes length), copies
`Camera.main.transform.rotation` onto the bar, and deactivates it past `visibleRange` — 90 m, which
since [decision #14](docs/decisions.md) is a quarter of the turret's 360 m `detectionRange`, so the
turret opens fire long before its own bar appears. The scene also enlarges the bar to 2.7 × 0.33 m
at `heightOffset: 4.8` (script defaults: 1.8 × 0.22 at 3.2). Like the HUD
it reads read-only properties (`MaxHealth`/`CurrentHealth`/`HealthFraction`) and there is still no
event bus. `EnemyTurret` sets `currentHealth` in **`Awake()`** for this: `Start()` order between two
components on one object is undefined, so a `Start()` init would show an empty bar for a frame. The
fill uses anchors rather than `Image.Type.Filled`, which would need a sprite — every graphic here is
a spriteless quad.
`Tools > DroneMissile > Build Turret Health Bars` (`Assets/Editor/TurretHealthBarBuilder.cs`) adds
the component to every `EnemyTurret` in the open scene. The drone still has no health bar and no
health — [decisions #10 and #12](docs/decisions.md) are unchanged.

**The wave run lives in `Arena.unity` and `RunManager` owns death there** ([decision
#23](docs/decisions.md)). `Assets/Editor/ArenaBuilder.cs` (`Tools > DroneMissile > Build Arena`,
needs Build Turret Types first) copies SampleScene, removes terrain/pad/turret, builds a 300 × 300 ×
80 m box from cubes (floor top at y = 0; walls + ceiling carry the empty `ArenaWall` marker and cast
no shadows), sets the drone's `reloadSceneOnDeath = false`, and adds `RunManager` (prefab refs, wave
table, arena size) and `RunUI`. It also creates `Turret_Boss.prefab` (grey ×3, 600 HP, `BossTurret`)
and `enemy_boss_rock.prefab` (sweep with `sweepRadius` 1.5) only if missing.

- **Death:** with `reloadSceneOnDeath` off, `DroneControls` stays crashed; `RunManager` polls
  `DeathDelayElapsed`, then Easy → `drone.ResetTo(spawn)` + restart the wave, Normal and Hardcore →
  end screen. Hardcore ([decision #25](docs/decisions.md)) is Normal without upgrades: rules that
  differ only on Easy test for Easy explicitly, and a cleared wave goes to `State.Banner` (2 s, no
  pause) instead of `Intermission`. Future card code must check `GameSession.UpgradesEnabled`.
  `DroneHUD` clears its death message when `HasCrashed` goes false again. On Easy,
  `OnCollisionEnter` stuns instead of crashing when the collider has `ArenaWall` (#20).
- **Waves:** `RunManager.waves` (Inspector table). Turrets spawn at random floor points and are
  lifted by renderer bounds (`RunManager.RestOnGround`) because prefab roots are not at their base.
  A wave is cleared when the live list is empty **and** `ColorBall.InFlight == 0`; boss balls
  `Register()` the turrets they spawn. Pauses use `Time.timeScale = 0`; `Shoting` refuses to fire
  then. Run stats and best wave (per difficulty, `PlayerPrefs`) live in `GameSession`;
  `EnemyTurret.TakeDamage` reports the HP actually removed.
- **Rings:** `YellowRing` builds a hoop of collider-less cubes plus one trigger box; passing it calls
  `Shoting.AddBonus(1)` (stacking). `Shoting.Fire` adds the bonus to `RocketProjectile.damage`
  (default 10 — no longer hardcoded) and boosted rockets blink via a property block. Both projectile
  `OnTriggerEnter`s now ignore other triggers, so rings and projectiles don't detonate each other.
- **UI:** `UiKit` (static) holds the shared canvas/button/text builders and `UiStyle`; `MainMenu` and
  `RunUI` both use it. `RunUI` (sorting order 200, above `DroneHUD`) shows the wave counter, ring
  bonus, the boss bar, the between-wave panel and the RUN OVER / VICTORY end screen.

**The main menu is a scene of its own, built in code like the HUD — but it takes input.**
`MainMenu.unity` and `Sandbox.unity` are **copies of `SampleScene`** made by
`Assets/Editor/MainMenuBuilder.cs` (`Tools > DroneMissile > Build All Menu Scenes`, or the
per-scene items), never hand-authored. The copies are snapshots: edits to SampleScene do not reach
them until the builder is rerun, which replaces the copy (after a confirmation dialog). The menu
copy is stripped for use as a backdrop — no `HUD`, the drone loses `DroneControls`, `Shoting`,
`DroneStunArcs` and its `Rigidbody` and is retagged `Untagged` (so `EnemyTurret` finds no target
and stays silent), and `Main Camera` is unparented and circles the drone via `MenuCameraOrbit`.
`MainMenu` builds a screen-space canvas with three panels (main, difficulty, upgrades placeholder)
using the same CanvasScaler/legacy-`Text` setup as `DroneHUD`, plus what the HUD deliberately lacks:
a `GraphicRaycaster` and an `EventSystem` with **`InputSystemUIInputModule`** — the project runs
the new Input System only, so the legacy `StandaloneInputModule` would throw. It reselects the open
panel's first button whenever the selection is lost, so arrows/Enter always work. Scene names live
in the static `GameSession` (with the chosen `Difficulty`, which nothing in gameplay reads yet, and
`HasSave`, always false — Continue is greyed out). A scene loaded by name must also be in the build
list (`Tools > DroneMissile > Configure Build Scenes`). `ReturnToMenu` (Esc → menu) sits in
SampleScene and Sandbox as a **temporary stopgap** until the ESC pause menu exists. All
[decision #21](docs/decisions.md).

**The HUD builds itself in code and polls.** `DroneHUD` is the only screen-space UI in gameplay;
the turret bar above is the only world-space one. It finds the
drone by the `Player` tag in `Start()`, then constructs its whole hierarchy — Canvas (screen-space
overlay), a full-screen `Image` for the stun vignette, four `Image` ticks for the crosshair, a `Text`
for the death message — at runtime. Nothing UI-shaped exists in `SampleScene.unity` except the empty
`HUD` GameObject that carries the component, placed by `Tools > DroneMissile > Build HUD`. The
vignette sprite is a procedurally generated `Texture2D` sized to the screen aspect, white with the
shape in its alpha, so the Inspector colour tints it live. `Update()` polls
`DroneControls.IsStunned` / `.HasCrashed` — read-only properties added for exactly this; there is
still no event bus. Deliberate, all [decision #12](docs/decisions.md): no `EventSystem` and no
`GraphicRaycaster` (the HUD never takes input, `raycastTarget` is off everywhere), legacy
`UnityEngine.UI.Text` with the builtin `LegacyRuntime.ttf` rather than TextMeshPro (which would drag
TMP Essential Resources into the repo), and the border reflects *actual* stun only — hits rejected
by `stunImmunity` show nothing.

**The crosshair is a static screen-centre cross, and that is exact rather than approximate.**
`Main Camera` and the player's `FirePoint` are siblings under `Drone` with **bit-identical** local
rotations (`{-0.3007058, 0, 0, 0.9537170}`, a 35° uptilt since [decision #24](docs/decisions.md);
it was 25° before), and the drone root is unrotated — so the optical axis and the muzzle axis are
parallel to 0.000°, with zero lateral offset. The muzzle sits 3.9 mm above the optical axis: 0.022°
of error at 10 m, less further out. Change the tilt only with `Tools > DroneMissile > Apply Camera
Uptilt` (`CameraTiltBuilder.UptiltDegrees`), which sets both transforms to one quaternion. `RocketProjectile` is
kinematic with gravity off and constant speed, so the shot is a straight ray. Screen centre *is* the
impact point at every range. Do not "improve" this with a raycast, a world-space marker, or lead
computation — all three are ruled out by [decision #12](docs/decisions.md). It stops being true only
if someone edits `FirePoint`'s rotation away from the camera's. The cross deliberately shows no
cooldown and stays lit while stunned, because `Shoting` never consults `DroneControls` and firing
genuinely still works during a stun.

**Projectiles are kinematic triggers, driven by `MovePosition`.** Both move via
`rb.MovePosition(rb.position + transform.forward * speed * Time.fixedDeltaTime)` in `FixedUpdate`
and detect hits with `OnTriggerEnter`. A projectile prefab therefore needs a `Collider` *and* a
kinematic `Rigidbody`: the trigger message only fires against the turret's static colliders because
a Rigidbody is present. Lifetime is a `Destroy(gameObject, lifeTime)` timer.

**`Awake()`**, not `Start()`, sets `isKinematic`, `useGravity`, `interpolation` and `isTrigger`
**from code**, which overrides the prefab; the code is authoritative. `Awake` runs synchronously
inside `Instantiate`, so no physics step can ever see the projectile as a non-trigger body —
kinematic-vs-*dynamic* **does** raise `OnCollisionEnter` (it is kinematic-vs-static that doesn't),
and since any collision now reloads the scene, a one-step window there would reset the run on every
shot. Don't move this back to `Start()`. Two further constraints hold this together, both in
[decision #8](docs/decisions.md) — don't undo either while "cleaning up":

- Motion must stay in `FixedUpdate`, not `Update()`. Trigger tests are discrete, so hit detection
  relies on the per-step displacement being shorter than the projectile's own collider (0.986 m
  along Z). In `Update()` that distance scales with frame rate and the bug returns below ~30 fps.
- `speed` must stay under **~49 m/s** for the same reason — *for anything still relying on the
  trigger.* Both prefabs now break that ceiling and they resolve it differently:
  - **`rocket.prefab` (`speed: 120`, not the 30 this file claimed until
    [decision #17](docs/decisions.md))** would tunnel through ~59% of its hits — 2.4 m per step
    against a 0.986 m collider. It no longer uses the trigger as its hit path: `FixedUpdate`
    **raycasts the segment it is about to cross** and detonates at `hit.point`. Detection is exact
    at any speed, and the sweep hands back a real surface normal for the impact effect. The trigger
    survives only as a fallback for something moving *into* the rocket, behind a `consumed` guard so
    the two paths cannot both fire.
  - **`enemy_rocket.prefab` (`speed: 1080`)** keeps tunnelling on purpose: 21.6 m per step, so it
    passes through the drone and the terrain unless a sample lands on it — roughly 6% of direct
    passes. That is [decision #14](docs/decisions.md), it is what makes the current tuning fun, and
    it is logged in [docs/backlog.md](docs/backlog.md) so nobody "fixes" it as a regression. Give it
    the same sweep only if reliable enemy hits are ever wanted.

**Drone flight** (`DroneControls`) is the one physics-driven component: `FixedUpdate` sums keyboard
and transmitter input, then `rb.AddForce(transform.up * throttle)` plus `rb.MoveRotation`. Thrust is
body-relative, so the drone tilts to translate — there is no stabilization or auto-level.

**Transmitter handling is duplicated.** `DroneControls.Start()` and `Shoting.Start()` each contain
their own copy of the device-discovery loop (prefer a `Joystick` whose name contains `Joystick1`,
else the last entry in `Joystick.all`). Axes are read by *string control path* through
`TryGetChildControl<AxisControl>` — `"z"` throttle, `"rx"` yaw, `"stick/y"` pitch, `"stick/x"` roll,
`"trigger"` fire — with pitch and roll negated and throttle remapped from `-1..1` to `0..1`.
A missing control returns `0f` silently, so a mismatched transmitter reads as "no input". These
paths are hardware-specific; `DroneControls.Start()` logs every device and control it finds, which
is the intended way to recalibrate for a different transmitter. Change an axis mapping in **both**
scripts.

Keyboard fallback: `LeftShift`/`LeftCtrl` throttle, `WASD` pitch/roll, `Q`/`E` yaw, `Space` fire.

## Working in this repo

**Scenes and prefabs are edited in the Unity editor, not as text.** `SampleScene.unity` and the
prefabs are YAML with GUID cross-references; hand-editing them corrupts references. Component values
visible in these files are Inspector overrides — changing a default in C# does not change what an
existing scene object or prefab uses.

**Every asset needs its `.meta` file committed alongside it.** The `.meta` holds the GUID that
scenes and prefabs reference; committing an asset without it, or deleting one, breaks those links.

**The YAML merge driver needs local setup after a fresh clone.** `.gitattributes` maps `*.unity`,
`*.prefab`, `*.asset`, `*.mat` and `*.meta` to a `unityyamlmerge` driver, but the driver itself is
defined in `.git/config`, which is not versioned. Without it, merges of those files fall back to a
line-based merge that produces broken YAML:

```bash
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver "'C:/Program Files/Unity/Hub/Editor/6000.5.10f1/Editor/Data/Tools/UnityYAMLMerge.exe' merge -p --force --fallback-onto-git %O %B %A"
```

**Tests.** `com.unity.test-framework` 1.7.0 is a dependency, but no tests and no test assembly exist.
Because the project has no `.asmdef` files, adding tests requires creating one (e.g.
`Assets/Tests/Tests.asmdef`) that references `UnityEngine.TestRunner` and `UnityEditor.TestRunner`
and is constrained to the editor/test platforms — otherwise `-runTests` finds nothing to run.

**`Assets/_Recovery/0.unity`** is a Unity crash-recovery snapshot that got committed. It is not part
of the build and is not the working scene; leave it alone or delete it deliberately.

**Generated project files are intentionally untracked.** `.gitignore` excludes `*.csproj`, `*.sln`
and `*.slnx`, so the root `Assembly-CSharp.csproj` and `DroneMissile.slnx` are local artifacts Unity
regenerates. `.vscode/settings.json` (tracked) points `dotnet.defaultSolution` at `DroneMissile.slnx`.
