
using Unity.AppUI.UI;
using Unity.VisualScripting;
using UnityEngine;

public class Enemy_Close : MonoBehaviour
{
    int damage = 30;
    public FieldOfView EnemyFOV;
    float speed = 2;
    private Transform player;
    Rigidbody2D rb;
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    public void OnCollisionEnter2D(Collision2D collision)
    {
        collision.gameObject.GetComponent<Player>().RegisterHit(-damage); 

        
    }
    public void Update()
    {
        
        if (EnemyFOV.IsChasing)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            rb.linearVelocity = direction * speed;
        }
        else { rb.linearVelocity = Vector2.zero; }

    }

}
