using UnityEngine;

public class RocketProjectile : MonoBehaviour
{
    public float speed = 30f;
    public float lifeTime = 5f;

    void Start()
    {
        Destroy(gameObject, lifeTime);

        GameObject drone = GameObject.FindGameObjectWithTag("Player");
        if (drone != null)
        {
            Collider rocketCol = GetComponent<Collider>();
            Collider[] droneCols = drone.GetComponentsInChildren<Collider>();
            foreach (var dc in droneCols)
            {
                Physics.IgnoreCollision(rocketCol, dc);
            }
        }
    }

    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Rocket hit: " + collision.gameObject.name);

        EnemyTurret turret = collision.gameObject.GetComponentInParent<EnemyTurret>();
        if (turret != null)
        {
            turret.TakeDamage(10);
        }

        Destroy(gameObject);
    }
}