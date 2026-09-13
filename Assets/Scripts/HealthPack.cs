using UnityEngine;

public class HealthPack : MonoBehaviour
{
    public int healAmount = 15;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Health hp = other.GetComponent<Health>();

            if (hp != null)
            {
                hp.Heal(healAmount);
            }

            Destroy(gameObject);
        }
    }
}
