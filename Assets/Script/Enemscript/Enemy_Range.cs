using Unity.VisualScripting;
using UnityEngine;

public class Enemy_Range : MonoBehaviour
{
    int hp;
    public int firerate;
    public Rigidbody2D rb;
    public Transform player;
    public FieldOfView EnemyFov;
    public void Start()
    {
        hp = 1;
        
        rb = GetComponent<Rigidbody2D>();
    }
    public void RegisterHit()
    {
        hp -= PlayerManagement.Instance.dmg;
        if (hp <= 0)
        {
            Destroy(gameObject);
        }
    }
    public void FixedUpdate()
    {
        if (EnemyFov.CanSeePlayer) 
        {
                Vector2 direction = (player.position - transform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        
    }
}
