using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class DroneControls : MonoBehaviour
{
    public float throttleForce = 15f;
    public float pitchSpeed = 100f;
    public float rollSpeed = 100f;
    public float yawSpeed = 100f;
    public float deadzone = 0.02f;

    [Header("Transmitter Calibration")]
    public bool useTransmitter = true;

    private Rigidbody rb;
    private Joystick transmitter;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

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

    void FixedUpdate()
    {
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

        rb.AddForce(transform.up * throttleInput * throttleForce, ForceMode.Force);

        Quaternion deltaRotation = Quaternion.Euler(
            pitchInput * pitchSpeed * Time.fixedDeltaTime,
            yawInput * yawSpeed * Time.fixedDeltaTime,
            rollInput * rollSpeed * Time.fixedDeltaTime
        );

        rb.MoveRotation(rb.rotation * deltaRotation);
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