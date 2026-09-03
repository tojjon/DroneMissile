using UnityEngine;

public class EnemyTurret : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform barrel;
    public Transform firePoint;

    public float detectionRange = 30f;
    public float fireRate = 2f;
    public float turnSpeed = 90f;

    [Header("Health")]
    public int maxHealth = 30;
    private int currentHealth;

    private Transform player;
    private float nextFireTime = 0f;

    void Start()
    {
        currentHealth = maxHealth;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > detectionRange) return;

        Vector3 direction = (player.position - barrel.position);
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            barrel.rotation = Quaternion.RotateTowards(barrel.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        if (Time.time >= nextFireTime)
        {
            Fire();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Fire()
    {
        if (projectilePrefab == null || firePoint == null) return;
        Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log("Turret took damage! Health: " + currentHealth);

        if (currentHealth <= 0)
        {
            Debug.Log("Turret destroyed!");
            Destroy(gameObject);
        }
    }
}