using UnityEngine;

public class Bullet : MonoBehaviour
{
    public LayerMask collisionMask;
    
    private float speed = 15f;
    void FixedUpdate()
    {
        // Move the shuriken forward (in 2D, "up" is usually forward)
        transform.Translate(Vector3.up * Time.deltaTime * speed);

      

        // Cast a ray forward in 2D
        RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.up, Time.deltaTime * speed + 0.1f, collisionMask);

        if (hit.collider != null)
        {
            // Reflect direction based on surface normal
            Vector2 reflectDir = Vector2.Reflect(transform.up, hit.normal);

            // Rotate to face the new direction
            float angle = Mathf.Atan2(reflectDir.y, reflectDir.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
