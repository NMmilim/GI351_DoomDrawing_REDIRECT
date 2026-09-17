

using UnityEngine;

public class melee_enem : Enemy
{

    public float movespeed;
    public Transform player;
void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    
    void FixedUpdate()
    {
        Vector2 direction =(player.position - transform.position).normalized;
        transform.position = Vector2.MoveTowards(transform.position, player.position, movespeed * Time.deltaTime);
    }
    
}
