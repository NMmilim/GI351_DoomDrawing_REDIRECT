using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public LayerMask collisionMask;
    
    
    public PlayerMovement _player;
    public float speed = 5;
    
    void FixedUpdate()
    {
        
        transform.Translate(Vector3.up * Time.deltaTime * speed);

      

        
        RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.up, Time.deltaTime * speed + 0.1f, collisionMask);

        if (hit.collider != null)
        {
            _player = hit.collider.GetComponent<PlayerMovement>();
            if (_player != null)
            {
                _player.RegisterHit();
                Destroy(gameObject);
                return;
            }
            

            
           
            
        }
    }
}
