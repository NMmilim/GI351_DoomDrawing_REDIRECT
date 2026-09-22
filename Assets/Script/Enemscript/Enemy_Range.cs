using Unity.VisualScripting;
using UnityEngine;

public class Enemy_Range : MonoBehaviour
{
    int hp;
    public GameObject bulletPre;
    public Transform firepos;
    public int firerate;
    public Rigidbody2D rb;
    public Transform player;
    public FieldOfView EnemyFov;
    private float nextFireTime = 1;
   
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
//public void FixedUpdate()
//    {
//        if (EnemyFov.CanSeePlayer && Time.time >= nextFireTime) 
//        {
//            nextFireTime = Time.time + Enemy_StatusManage.Instance.firerate;
//            Shoot();
//        }

//    }
    void Shoot()
    {
        GameObject bullet = Instantiate(bulletPre, firepos.position, transform.rotation);
        Rigidbody2D rbBullet = bullet.GetComponent<Rigidbody2D>();
        rbBullet.linearVelocity = transform.right * 10f; 
    }

}
