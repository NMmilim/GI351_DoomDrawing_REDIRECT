using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public LayerMask collisionMask;
    public float speed = 5;
    private int damage = 30;
    
    void FixedUpdate()
    {

        transform.Translate(Vector3.up * Time.deltaTime * speed);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.up, Time.deltaTime * speed + 0.1f, collisionMask);
        Player player;

        if (hit.collider != null)
        {
            player = hit.collider.GetComponent<Player>();
            if (player != null)
            {
                player.RegisterHit(damage);
                Destroy(gameObject);
                return;
            }

            Destroy(gameObject);
        }
    }
}
