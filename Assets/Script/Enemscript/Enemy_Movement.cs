using UnityEngine;

public class Enemy_Movement : MonoBehaviour
{
    private Rigidbody2D rb;
    [SerializeField]private Transform player;
    private bool IsChasing;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
    }

    // Update is called once per frame
    void Update()
    {
        if (IsChasing == true)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            rb.linearVelocity = direction * Enemy_StatusManage.Instance.speed;
        }
    }
public void OnTriggerEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player") 
        {
            IsChasing = true; 
        }
    
    }
    public void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            
            rb.linearVelocity = Vector2.zero;
            IsChasing = false;
        }
    }
}

