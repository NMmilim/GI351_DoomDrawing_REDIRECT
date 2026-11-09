
using UnityEngine;

public class Enemy_Close : MonoBehaviour
{
    public int damage = 30;
    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        Player hitPlayer = collision.gameObject.GetComponent<Player>();
        if (hitPlayer != null)
        {
            hitPlayer.RegisterHit(damage);
        }
    }
}

