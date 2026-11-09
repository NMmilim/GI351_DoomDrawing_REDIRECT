using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public LayerMask collisionMask;
    public float speed = 5f;
    public int damage = 30;

    private void FixedUpdate()
    {
        transform.Translate(Vector3.right * (Time.deltaTime * speed));
        RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.right, Time.deltaTime * speed + 0.1f, collisionMask);

        if (hit.collider != null)
        {
            Player player = hit.collider.GetComponent<Player>();
            if (player != null)
            {
                player.RegisterHit(damage);
            }

            Destroy(gameObject);
        }
    }
}

