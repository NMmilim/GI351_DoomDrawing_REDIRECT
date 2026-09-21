using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public LayerMask collisionMask;
    public int Damage = Enemy_StatusManage.Instance.damage;
    public float speed = 5;
    
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
                player.RegisterHit();
                Destroy(gameObject);
                return;
            }
            

            
           
            
        }
    }
}
