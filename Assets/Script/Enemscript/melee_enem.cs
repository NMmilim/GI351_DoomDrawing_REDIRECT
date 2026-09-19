

using Unity.VisualScripting;
using UnityEngine;

public class melee_enem : Enemy
{

    public float movespeed;
    public Transform player;
    EnemView enemView;
  //  public Animator MeleeAnim;
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        enemView = GetComponent<EnemView>();
        
    }
    void Update()
    {
        if (enemView.IsChasing == true)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            transform.position += (Vector3)direction * movespeed * Time.deltaTime;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90);
        }




    }
}
