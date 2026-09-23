using Unity.AppUI.UI;
using Unity.VisualScripting;
using UnityEngine;

public class Enemy_Range : MonoBehaviour
{
    int hp;
    public GameObject bulletPre;
    public Transform firepos;
    private int firerate;
    public Rigidbody2D rb;
    public Transform player;
    private float firecooldown = 0.333f;
    private float nextFireTime = 1;
   
    public void Start()
    {
        hp = 1;
        
        rb = GetComponent<Rigidbody2D>();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Vector2 direction = (player.position - transform.position).normalized;
        transform.right = direction;
        if (Time.time >= nextFireTime) {Shoot();nextFireTime = Time.time + firecooldown; }
        
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        Vector2 direction = Vector2.zero;
    }
    public void RegisterHit()
    {
        hp -= PlayerManagement.Instance.dmg;
        if (hp <= 0)
        {
            Destroy(gameObject);
        }
    }
    void Shoot()
    {
        GameObject bullet = Instantiate(bulletPre, firepos.position, transform.rotation);
        Rigidbody2D rbBullet = bullet.GetComponent<Rigidbody2D>();
        rbBullet.linearVelocity = transform.right * 10f;
        
    }

}
