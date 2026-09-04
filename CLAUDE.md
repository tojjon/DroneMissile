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
         -testFilter "DroneHealthTests.TakeDamage_ReducesHealth" -testResults results.xml
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
serialized public fields set in the Inspector.

**Tag contract** (breaking these silently disables gameplay — nothing throws):

| Tag | On | Read by |
|---|---|---|
| `Player` | the drone root | `EnemyTurret` (targeting), `EnemyProjectile` (damage) |
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
  `DroneHealth.TakeDamage(damage)`, where `damage` *is* a projectile field.

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

Only `EnemyTurret` actually dies (`Destroy(gameObject)`). `DroneHealth` at 0 HP just logs
`"Drone destroyed!"` — there is no death, respawn, or game-over path yet.

**Projectiles are kinematic triggers, driven by `MovePosition`.** Both move via
`rb.MovePosition(rb.position + transform.forward * speed * Time.fixedDeltaTime)` in `FixedUpdate`
and detect hits with `OnTriggerEnter`. A projectile prefab therefore needs a `Collider` *and* a
kinematic `Rigidbody`: the trigger message only fires against the turret's static colliders because
a Rigidbody is present. Lifetime is a `Destroy(gameObject, lifeTime)` timer.

`Start()` sets `isKinematic`, `useGravity`, `interpolation` and `isTrigger` **from code**, which
overrides the prefab — the prefabs still read `Is Kinematic ✔ / Continuous` in the Inspector, and
the code is authoritative. Two constraints hold this together, both explained in
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
