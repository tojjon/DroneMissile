using UnityEngine;

public class DroneHealth : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log("Drone took damage! Health: " + currentHealth);

        if (currentHealth <= 0)
        {
            Debug.Log("Drone destroyed!");
        }
    }
}