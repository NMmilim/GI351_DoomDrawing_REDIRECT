using UnityEngine;

public class RangeEnemy : MonoBehaviour
{
    public Transform player;
    public GameObject bulletPrefab;
    public Transform firePoint;

    public float fireRate = 3f;
    private float nextFireTime = 0f;

    public float detectionRange = 15f;

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= detectionRange)
        {
            AimAtPlayer();

            if (Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + fireRate;
            }
        }
    }

    void AimAtPlayer()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0;

        if (dir != Vector3.zero)
            transform.forward = dir.normalized;
    }

    void Shoot()
    {
        if (bulletPrefab == null || firePoint == null) return;

        Vector3 dir = (player.position - firePoint.position).normalized;

        GameObject bullet = Instantiate(
            bulletPrefab,
            firePoint.position,
            Quaternion.LookRotation(dir)
        );

        
        Collider enemyCol = GetComponent<Collider>();
        Collider bulletCol = bullet.GetComponent<Collider>();

        if (enemyCol != null && bulletCol != null)
        {
            Physics.IgnoreCollision(enemyCol, bulletCol);
        }
    }
}