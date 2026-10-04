using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class Shoting : MonoBehaviour
{
    public GameObject rocketPrefab;
    public Transform firePoint;
    public float fireRate = 0.5f; // seconds between shots

    private Joystick transmitter;
    private float nextFireTime = 0f;

    // Ring bonus for the next shot, stacking: each yellow ring adds 1 (docs/design/waves.md).
    private int pendingBonus;
    public int PendingBonus => pendingBonus;

    public void AddBonus(int amount)
    {
        pendingBonus += amount;
    }

    void Start()
    {
        // Reuse the same device-finding logic as DroneControls
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
    }

    void Update()
    {
        // Paused (between-wave screen, end screen): Time.time stands still, so the cooldown would
        // let exactly one rocket out into the frozen world.
        if (Time.timeScale == 0f) return;

        bool triggerPressed = false;

        // Transmitter trigger
        if (transmitter != null)
        {
            var trigger = transmitter.TryGetChildControl<ButtonControl>("trigger");
            if (trigger != null) triggerPressed = trigger.isPressed;
        }

        // Keyboard fallback (Spacebar)
        if (Keyboard.current.spaceKey.isPressed) triggerPressed = true;

        if (triggerPressed && Time.time >= nextFireTime)
        {
            Fire();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Fire()
    {
        if (rocketPrefab == null || firePoint == null)
        {
            Debug.LogWarning("Rocket prefab or FirePoint not assigned!");
            return;
        }

        GameObject rocket = Instantiate(rocketPrefab, firePoint.position, firePoint.rotation);

        // Tell the rocket who fired it so it can filter out self-hits. This script sits on the
        // drone root, which is exactly the hierarchy the rocket must ignore - it spawns partly
        // inside the drone's own collider.
        RocketProjectile proj = rocket.GetComponent<RocketProjectile>();
        if (proj != null)
        {
            proj.owner = transform;
            proj.damage += pendingBonus;  // spent on this one shot, boosted rockets blink
            pendingBonus = 0;
        }
    }
}