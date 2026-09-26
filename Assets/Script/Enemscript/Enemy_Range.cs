using Unity.AppUI.UI;
using Unity.VisualScripting;
using UnityEngine;

public class Enemy_Range : MonoBehaviour
{
    int hp;
    public GameObject bulletPre;
    public Transform firepos;
    public Rigidbody2D rb;
    public Transform player;
    public Transform targetPos;
    private EnemyFOV _fov;
    [Header("Shooting")]
    private float firecooldown = 0f;
    public float firerate = 1.5f;


    void Awake()
    {
        _fov = GetComponentInChildren<EnemyFOV>();

    }

    public void Start()
    {
        hp = 1;

        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (firecooldown > 0f)
        {
            firecooldown -= Time.deltaTime;

        }

        if (_fov != null && _fov.PlayerInSight && firecooldown <= 0f)
        {
            Shoot();
            firecooldown = firerate;
        }

    }
    public void RegisterHit(int amount)
    {
        hp -= amount;
        if (hp <= 0)
        {
            Destroy(gameObject);
        }
    }
    void Shoot()
    {

        Vector2 shootDir = ((Vector2)_fov.DetectedPlayer.position - (Vector2)firepos.position).normalized;


        float angle = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg;

        GameObject bullet = Instantiate(bulletPre, firepos.position, Quaternion.Euler(0f, 0f, angle));
        Rigidbody2D rbBullet = bullet.GetComponent<Rigidbody2D>();
        rbBullet.linearVelocity = transform.right * 10f;
    }

}
