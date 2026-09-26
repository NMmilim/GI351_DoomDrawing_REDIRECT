using UnityEngine;

public class Bullet : MonoBehaviour
{
    public LayerMask collisionMask;
    public GameObject bloodEffectPrefab;
    public int maxBounce = 1;
    private int bounce = 0;
    public float speed = 20;
    
    
    void FixedUpdate()
    {
        
        transform.Translate(Vector3.up * Time.deltaTime * speed);
        
        RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.up, Time.deltaTime * speed + 0.1f, collisionMask);

        if (hit.collider != null)
        {
            Enemy_Range enemy = hit.collider.GetComponent<Enemy_Range>();
            Player player = hit.collider.GetComponent<Player>();
            if (enemy != null)
            {
                enemy.RegisterHit(PlayerManagement.Instance.dmg);
                Destroy(gameObject);

                Instantiate(bloodEffectPrefab, hit.point, Quaternion.identity);
                return;

            }
            
            //if (player != null)
            //{
            //    player.RegisterHit();
            //    Destroy(gameObject);
            //    return;
            //}
            Vector2 reflectDir = Vector2.Reflect(transform.up, hit.normal);

            
            float angle = Mathf.Atan2(reflectDir.y, reflectDir.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
            bounce++;
            if (bounce >= maxBounce)
            {
                Destroy(gameObject);
            }
        }
    }
}
