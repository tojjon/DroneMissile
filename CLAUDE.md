# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Unity 3D game: a player-piloted drone fights ground turrets over terrain. Flight input comes from a
physical RC transmitter (exposed as a HID joystick) with a keyboard fallback.

- Unity **6000.5.10f1** (pinned in `ProjectSettings/ProjectVersion.txt`; the editor must match exactly)
- **URP** (Universal Render Pipeline) 17.5.0 — PC and Mobile render pipeline assets under `Assets/Settings/`
- **Input System** 1.20.0 (the new package, not legacy `Input`) — `Assets/InputSystem_Actions.inputactions`
- One scene in the build: `Assets/Scenes/SampleScene.unity`
- Installed editor path: `C:/Program Files/Unity/Hub/Editor/6000.5.10f1/`

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
```

**Only one process may hold the project at a time.** Unity locks `Library/`, so every CLI command
above fails while the project is open in the editor. Close the editor first, or expect
`Multiple Unity instances cannot open the same project`.

Test results land in `results.xml` (NUnit format) — parse that, not stdout. `-logFile -` sends the
editor log to stdout; without it the log goes to `Logs/` (gitignored).

## Architecture

Six `MonoBehaviour` scripts in `Assets/scripts/`, no assembly definitions — everything compiles into
the default `Assembly-CSharp`. There is no manager, service locator, or event bus; components find
each other at runtime through **Unity tags**, and are wired to prefabs/scene objects through
serialized public fields set in the Inspector. `Assets/Editor/` holds editor-only menu items that
build scene objects (`Tools > DroneMissile > ...`) — that is how scene geometry and the HUD object
get created here, since scene YAML is never hand-edited.

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

- Player: `Shoting` instantiates `rocket.prefab` (`RocketProjectile`) → on trigger walks
  `GetComponentInParent<EnemyTurret>()` and calls `TakeDamage(10)` — damage is hardcoded at the
  call site, not a field on the projectile.
- Enemy: `EnemyTurret` tracks the player within `detectionRange`, rotates `barrel` toward it, and
  instantiates `enemy_rocket.prefab` (`EnemyProjectile`) → on trigger calls
  `DroneControls.Stun(stunDuration)`. The drone has no health, so a hit takes control away instead
  of dealing damage — no thrust and no `MoveRotation` for a second, so it holds its attitude and
  falls. `stunDuration` *is* a projectile field; the i-frames that stop a 10 shot/s turret from
  pinning the drone (`stunImmunity`) live on `DroneControls`.

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
- `stunImmunity` — i-frames. At the scene's `fireRate: 0.1` a refreshing 1 s stun would leave the
  drone limp until it hit the ground.

The drone also runs `ContinuousDynamic` CCD and `Interpolate`, set from code in `Start()`
([decision #11](docs/decisions.md)). Decision #8's "CCD is a no-op" applies to the *kinematic
projectiles* — the drone is dynamic, and with `Discrete` its 0.387 m collider falls straight through
the terrain heightfield above ~19.4 m/s, which one drop off the pad exceeds.

Only `EnemyTurret` dies in the ordinary sense (`Destroy(gameObject)`). There is no win condition.

**The HUD builds itself in code and polls.** `DroneHUD` is the only UI in the project. It finds the
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
rotations (`{-0.21643952, 0, 0, 0.97629607}`, -25° pitch), and the drone root is unrotated — so the
optical axis and the muzzle axis are parallel to 0.000°, with zero lateral offset. The muzzle sits
4.15 mm below the optical axis: 0.024° of error at 10 m, less further out. `RocketProjectile` is
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
- `speed` must stay under **~49 m/s** for the same reason. Faster projectiles tunnel again.

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
