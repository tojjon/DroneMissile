using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public float speed = 20f;
    public float lifeTime = 5f;
    public int damage = 10;

    void Start()
    {
        Destroy(gameObject, lifeTime);

        // Ignore collision with whatever fired it (any Enemy)
        Collider ownCol = GetComponent<Collider>();
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var enemy in enemies)
        {
            Collider[] enemyCols = enemy.GetComponentsInChildren<Collider>();
            foreach (var ec in enemyCols)
            {
                Physics.IgnoreCollision(ownCol, ec);
            }
        }
    }

    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            DroneHealth health = collision.gameObject.GetComponent<DroneHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
        }
        Destroy(gameObject);
    }
}