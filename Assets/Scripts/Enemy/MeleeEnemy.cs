using UnityEngine;

public class MeleeEnemy : MonoBehaviour
{
    public int damage = 10;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Health player = collision.gameObject.GetComponent<Health>();

            if (player != null)
            {
                player.TakeDamage(damage);
            }
        }
    }
}