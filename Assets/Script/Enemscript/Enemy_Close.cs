

using JetBrains.Annotations;
using Unity.AppUI.UI;
using Unity.VisualScripting;
using UnityEngine;

public class Enemy_Close : Enemy_Range
{
   
    
    public void OnCollisionEnter2D(Collision2D collision)
    {
        collision.gameObject.GetComponent<Player>().MeleeRegisterHit();
    }
    public void FixedUpdate()
    {
        //if (EnemyFov.CanSeePlayer)
        //{
        //    Vector2 direction = (player.position - transform.position).normalized;
        //    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        //    transform.rotation = Quaternion.Euler(0, 0, angle);
        //    rb.linearVelocity = direction * Enemy_StatusManage.Instance.speed;
        //}
        
    //if (!EnemyFov.CanSeePlayer)
    //    {
    //        rb.linearVelocity = Vector2.zero;
    //    }
    
    }
  
}
