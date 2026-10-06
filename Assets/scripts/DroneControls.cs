using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;

public class DroneControls : MonoBehaviour
{
    // Which look a stun gets. The HUD border shows for every kind; each DroneStunArcs instance plays
    // only for its own kind. Chosen per projectile prefab - see docs/decisions.md #22.
    public enum StunKind { Rock, Electric }

    public float throttleForce = 15f;
    public float pitchSpeed = 100f;
    public float rollSpeed = 100f;
    public float yawSpeed = 100f;
    public float deadzone = 0.02f;

    [Header("Air")]
    [Tooltip("Quadratic air drag, per metre (acceleration = k * speed^2). Replaces the Rigidbody's linear damping, which acted like syrup at low speed: 0.3 took 15% of gravity off at 5 m/s. At 0.015 a fall tops out near 31 m/s and full-thrust flight near 60 m/s, but a slow drone feels almost no drag. See docs/decisions.md #30.")]
    public float quadraticDrag = 0.015f;

    [Tooltip("Gravity on the drone as a multiple of Physics.gravity. Above 1 the drone drops harder and climbs a little slower - throttleForce is not scaled with it.")]
    public float gravityMultiplier = 1.5f;

    [Header("Transmitter Calibration")]
    public bool useTransmitter = true;

    [Header("Crash / Reset")]
    [Tooltip("The drone must climb this far above its spawn height before a collision counts as a crash.")]
    public float armAltitude = 0.5f;

    [Tooltip("Falling below this Y reloads the scene even if no collision was reported. Terrain tiles sit at y = -57.")]
    public float killY = -70f;

    [Tooltip("How long the death message stays up before the scene reloads. Physics keeps running.")]
    public float deathDelay = 1f;

    [Tooltip("Off in the Arena: there RunManager decides what death means (restart the wave on Easy, end screen on Normal) and the scene must not reload under it. See docs/decisions.md #23.")]
    public bool reloadSceneOnDeath = true;

    [Tooltip("Easy only: hitting an ArenaWall (walls, ceiling) stuns this long instead of killing. See docs/decisions.md #20.")]
    public float wallStun = 1f;

    [Header("Stun")]
    [Tooltip("How long the drone cannot be stunned again after a stun ends.")]
    public float stunImmunity = 2f;

    [Header("Block (upgrade)")]
    [Tooltip("How long one block shrugs off every projectile.")]
    public float blockDuration = 1f;

    [Tooltip("Seconds to refill one used block charge. Charges = Block cards owned.")]
    public float blockRecharge = 8f;

    private Rigidbody rb;
    private Joystick transmitter;

    // Block upgrade (docs/plans/upgrades-2.md). One recharge timer refills one charge at a time.
    private float blockUntil;
    private int blockCharges;
    private int knownMaxCharges;
    private bool recharging;
    private float rechargeAt;

    // Crash state. `armed` is a one-way latch - see the comment in FixedUpdate.
    private bool armed;
    private bool hasCrashed;
    private float reloadAt; // absolute Time.time, like the stun deadlines; valid once hasCrashed
    private float spawnY;

    // Stun deadlines. Both are absolute Time.time values, never countdowns. See Stun().
    private float stunnedUntil;
    private float stunnableAgainAt;

    // Read-only state for DroneHUD. Stun is a pair of deadlines rather than a flag, so there is
    // nothing meaningful for the HUD to poll without this; hasCrashed stays private because only
    // Crash() may ever set it.
    public bool IsStunned => !hasCrashed && Time.time < stunnedUntil;
    public bool HasCrashed => hasCrashed;

    // True once the death message has had its beat. RunManager waits for this before acting, so the
    // arena keeps the same rhythm as the reload path.
    public bool DeathDelayElapsed => hasCrashed && Time.time >= reloadAt;

    // What the pilot is commanding this physics step, after clamping: throttle 0..1, the rest -1..1.
    // Zero while stunned or crashed - the motors are off. Read by DroneMotorSound.
    public float ThrottleInput { get; private set; }
    public float PitchInput { get; private set; }
    public float RollInput { get; private set; }
    public float YawInput { get; private set; }

    // Block state for DroneHUD (yellow screen edge, charge dots).
    public bool IsBlocking => !hasCrashed && Time.time < blockUntil;
    public int BlockCharges => blockCharges;
    public int MaxBlockCharges => GameSession.Stacks(UpgradeIds.Block);

    // Kind of the stun currently running (or the last one). Only meaningful while IsStunned.
    public StunKind LastStunKind { get; private set; }

    void Awake()
    {
        spawnY = transform.position.y;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Continuous CCD, set from code so it cannot be lost in the Inspector. With Discrete the
        // 0.387 m tall collider passes straight through the terrain heightfield above ~19.4 m/s
        // (collider height / 0.02 s step) - and simply falling off the pad arrives at ~25 m/s.
        // Not ContinuousSpeculative: it generates phantom contacts across adjacent heightfield
        // triangles, and here one phantom contact is a silent scene reload.
        // This does not contradict decisions.md #8 - that rules CCD out on the *kinematic*
        // projectiles, where it is a no-op. The drone is dynamic. See decisions.md #11.
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Drag comes from quadraticDrag in FixedUpdate. Zeroed from code like CCD above, so the 0.3
        // still saved on the Rigidbody in every scene cannot come back (decisions.md #30).
        rb.linearDamping = 0f;

        foreach (var device in InputSystem.devices)
        {
            Debug.Log("Found device: " + device.displayName + " | " + device.name);
        }

        foreach (var joystick in Joystick.all)
        {
            if (joystick.displayName.Contains("Joystick1") || joystick.name.Contains("Joystick1"))
            {
                transmitter = joystick;
                break;
            }
        }

        if (transmitter == null && Joystick.all.Count > 0)
        {
            transmitter = Joystick.all[Joystick.all.Count - 1];
        }

        if (transmitter != null)
        {
            Debug.Log("=== All controls on " + transmitter.displayName + " ===");
            foreach (var control in transmitter.allControls)
            {
                Debug.Log(control.name + " | path: " + control.path + " | type: " + control.GetType().Name);
            }
        }

        if (transmitter == null && useTransmitter)
        {
            Debug.LogWarning("No joystick/transmitter detected. Falling back to keyboard only.");
        }
        else if (transmitter != null)
        {
            Debug.Log("Using transmitter device: " + transmitter.displayName);
        }
    }

    // Block input lives in Update, not FixedUpdate: wasPressedThisFrame is a per-frame edge and a
    // physics step can miss it or see it twice.
    void Update()
    {
        UpdateBlockCharges();

        // Paused (between waves, sandbox console - where F is typed into the field) or dead.
        if (hasCrashed || Time.timeScale == 0f) return;

        // Keyboard F for now; Viktor maps a transmitter switch later (docs/plans/upgrades-2.md).
        bool pressed = Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
        if (pressed && blockCharges > 0 && !IsBlocking)
        {
            blockCharges--;
            blockUntil = Time.time + blockDuration;
        }
    }

    void UpdateBlockCharges()
    {
        // A new card arrives charged; a card removed in the sandbox takes its charge with it.
        int max = MaxBlockCharges;
        if (max != knownMaxCharges)
        {
            blockCharges = Mathf.Clamp(blockCharges + max - knownMaxCharges, 0, max);
            knownMaxCharges = max;
        }

        if (blockCharges >= max)
        {
            recharging = false;
        }
        else if (!recharging)
        {
            recharging = true;
            rechargeAt = Time.time + blockRecharge;
        }
        else if (Time.time >= rechargeAt)
        {
            blockCharges++;
            recharging = false;
        }
    }

    void FixedUpdate()
    {
        // Air and the extra gravity act always - stunned, crashed or flying - so a wreck falls the
        // same way a live drone does. ForceMode.Acceleration: both are independent of the 7.5 g mass.
        Vector3 v = rb.linearVelocity;
        rb.AddForce(-quadraticDrag * v.magnitude * v, ForceMode.Acceleration);
        if (rb.useGravity) rb.AddForce(Physics.gravity * (gravityMultiplier - 1f), ForceMode.Acceleration);

        // Crashed: input is dead, but physics keeps stepping so the wreck tumbles under the death
        // message. Nothing here can fire twice - OnCollisionEnter tests !hasCrashed, the kill plane
        // check sits below this return, and Stun() bails on hasCrashed.
        if (hasCrashed)
        {
            SetInputs(0f, 0f, 0f, 0f);
            if (reloadSceneOnDeath && Time.time >= reloadAt) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        // Kill plane, deliberately NOT gated on `armed`: the ground slab is a floating 23 x 29 m
        // platform and the terrain sits 57 m below it. Drifting off the edge before ever arming is
        // a fall the collision path can miss, and nothing in the project can restart a run.
        if (transform.position.y < killY)
        {
            Crash();
            return;
        }

        // Arming latch. Without it the drone reloads the scene about once a second forever: it
        // spawns resting on the pad, so the very first contact would fire at t = 0. A grace *timer*
        // only works by accident, and "armed once throttle is commanded" arms immediately whenever
        // the transmitter throttle stick is not parked at the bottom - (rawThrottle + 1) / 2 is
        // never negative.
        if (!armed && transform.position.y > spawnY + armAltitude) armed = true;

        // Stunned: no thrust and no MoveRotation, so the drone holds its last attitude and falls.
        if (Time.time < stunnedUntil)
        {
            SetInputs(0f, 0f, 0f, 0f);
            return;
        }

        float throttleInput = 0f;
        float pitchInput = 0f;
        float rollInput = 0f;
        float yawInput = 0f;

        // --- Keyboard controls (fallback / testing) ---
        if (Keyboard.current.leftShiftKey.isPressed) throttleInput += 1f;
        if (Keyboard.current.leftCtrlKey.isPressed) throttleInput -= 1f;

        if (Keyboard.current.wKey.isPressed) pitchInput += 1f;
        if (Keyboard.current.sKey.isPressed) pitchInput -= 1f;

        if (Keyboard.current.aKey.isPressed) rollInput += 1f;
        if (Keyboard.current.dKey.isPressed) rollInput -= 1f;

        if (Keyboard.current.qKey.isPressed) yawInput -= 1f;
        if (Keyboard.current.eKey.isPressed) yawInput += 1f;

        // --- Transmitter controls ---
        if (useTransmitter && transmitter != null)
        {
            float rawThrottle = ReadAxis("z");            // -1 (motors off) .. 1 (full thrust)
            float rawYaw = ReadAxis("rx");                 // -1 .. 1, centered at 0
            float rawPitch = -ReadAxis("stick/y");         // -1 .. 1, centered at 0 (inverted)
            float rawRoll = -ReadAxis("stick/x");          // -1 .. 1, centered at 0 (inverted)

            if (Mathf.Abs(rawYaw) > deadzone) yawInput += rawYaw;
            if (Mathf.Abs(rawPitch) > deadzone) pitchInput += rawPitch;
            if (Mathf.Abs(rawRoll) > deadzone) rollInput += rawRoll;

            throttleInput += (rawThrottle + 1f) / 2f; // remap -1..1 to 0..1 (0% to 100% thrust)
        }

        throttleInput = Mathf.Clamp(throttleInput, 0f, 1f);
        pitchInput = Mathf.Clamp(pitchInput, -1f, 1f);
        rollInput = Mathf.Clamp(rollInput, -1f, 1f);
        yawInput = Mathf.Clamp(yawInput, -1f, 1f);
        SetInputs(throttleInput, pitchInput, rollInput, yawInput);

        rb.AddForce(transform.up * throttleInput * throttleForce, ForceMode.Force);

        Quaternion deltaRotation = Quaternion.Euler(
            pitchInput * pitchSpeed * Time.fixedDeltaTime,
            yawInput * yawSpeed * Time.fixedDeltaTime,
            rollInput * rollSpeed * Time.fixedDeltaTime
        );

        rb.MoveRotation(rb.rotation * deltaRotation);
    }

    void SetInputs(float throttle, float pitch, float roll, float yaw)
    {
        ThrottleInput = throttle;
        PitchInput = pitch;
        RollInput = roll;
        YawInput = yaw;
    }

    // Any solid contact ends the run - terrain, the ground slab, a turret. The drone has no health;
    // see decisions.md #10. Projectiles never reach this: both are trigger colliders.
    void OnCollisionEnter(Collision collision)
    {
        if (!armed || hasCrashed) return;

        // Arena walls and ceiling only stun on Easy (docs/decisions.md #20); the floor and turrets
        // carry no ArenaWall, so they still kill on both difficulties. A marker component rather than
        // a tag - tags are a silent contract here (docs/reference/unity-gotchas.md).
        if (GameSession.CurrentDifficulty == GameSession.Difficulty.Easy &&
            collision.collider.GetComponent<ArenaWall>() != null)
        {
            Stun(wallStun, StunKind.Rock, blockable: false);   // a wall is not a projectile
            return;
        }

        Crash();
    }

    // Easy restarts a wave without reloading the scene: put the drone back on its spawn as if it had
    // just been loaded. The arming latch re-arms from the new height, exactly like a fresh scene.
    public void ResetTo(Vector3 position, Quaternion rotation)
    {
        hasCrashed = false;
        armed = false;
        stunnedUntil = 0f;
        stunnableAgainAt = 0f;
        blockUntil = 0f;
        blockCharges = MaxBlockCharges;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = position;
        rb.rotation = rotation;
        transform.SetPositionAndRotation(position, rotation);
        spawnY = position.y;
    }

    // Takes control away for `seconds`. Called by EnemyProjectile (and its subclasses) on a hit.
    // `blockable`: projectiles are, an arena wall on Easy is not. A blocked hit is dropped whole -
    // no stun and no i-frames - and every projectile type comes through here, so the Block upgrade
    // needs no check of its own in the projectiles.
    public void Stun(float seconds, StunKind kind, bool blockable = true)
    {
        if (hasCrashed || Time.time < stunnableAgainAt) return;
        if (blockable && IsBlocking) return;

        // Set only past the i-frame check, so a rejected hit cannot repaint a stun already running.
        LastStunKind = kind;

        // A deadline, not a countdown: Stun() runs inside the physics step, so a value decremented
        // in FixedUpdate would gain or lose a whole step depending on ordering. Mathf.Max so a short
        // stun can never truncate a longer one already running.
        stunnedUntil = Mathf.Max(stunnedUntil, Time.time + seconds);

        // I-frames. The scene sets EnemyTurret.fireRate to 0.1 (ten shots a second) - without this,
        // entering turret range means being stunned continuously until the drone hits the ground.
        stunnableAgainAt = stunnedUntil + stunImmunity;
    }

    // Arms the death timer instead of reloading on the spot. FixedUpdate performs the reload once
    // `reloadAt` passes, which buys DroneHUD a beat to show the death message while the wreck falls.
    void Crash()
    {
        hasCrashed = true;
        reloadAt = Time.time + deathDelay;
        Debug.Log("Drone crashed - reloading in " + deathDelay + " s.");
    }

    float ReadAxis(string controlName)
    {
        if (transmitter == null) return 0f;

        var control = transmitter.TryGetChildControl<AxisControl>(controlName);
        if (control == null)
        {
            return 0f;
        }
        return control.ReadValue();
    }
}
