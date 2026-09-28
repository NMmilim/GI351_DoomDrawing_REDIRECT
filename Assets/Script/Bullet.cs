using UnityEngine;

public class Bullet : MonoBehaviour
{
    public LayerMask collisionMask;
    public GameObject bloodEffectPrefab;
    public int maxBounce = 1;
    private int bounce = 0;
    public float speed = 20;
    public GameObject[] bloodDecals;

    void FixedUpdate()
    {
        
        transform.Translate(Vector3.up * Time.deltaTime * speed);
        
        RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.up, Time.deltaTime * speed + 0.1f, collisionMask);

        if (hit.collider != null)
        {
            SoundManager.Instance.PlaySound3D("Ricochet", transform.position);
            Enemy_Range enemy = hit.collider.GetComponent<Enemy_Range>();
            Player player = hit.collider.GetComponent<Player>();
            if (enemy != null)
            {
                enemy.RegisterHit(PlayerManagement.Instance.dmg);
                SoundManager.Instance.PlaySound3D("BulletImpactFlesh", transform.position);
                
                Quaternion randomRot = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
                Instantiate(bloodEffectPrefab, hit.point, randomRot);
                int index = Random.Range(0, bloodDecals.Length);
                Instantiate(bloodDecals[index], hit.point, randomRot);
                Destroy(gameObject);
                return;
            }

            //if (player != null)
            //{
            //    player.RegisterHit();hj
            //    Destroy(gameObject);
            //    return;
            //}
            Vector2 reflectDir = Vector2.Reflect(transform.up, hit.normal);

            
            float angle = Mathf.Atan2(reflectDir.y, reflectDir.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
            bounce++;
            if (bounce >= maxBounce)
            {
                SoundManager.Instance.PlaySound3D("BulletImpact", transform.position);
                Destroy(gameObject);
            }
        }
    }
}
