using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 50;
    private int currentHealth;

    public GameObject healthPackPrefab;
    [Range(0f, 1f)]
    public float dropChance = 0.75f;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log($"{gameObject.name} HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log(gameObject.name + " ตายแล้ว");

        DropHealthPack();
        Destroy(gameObject);
    }

    void DropHealthPack()
    {
        if (healthPackPrefab == null) return;

        if (Random.value <= dropChance)
        {
            Instantiate(healthPackPrefab, transform.position, Quaternion.identity);
        }
    }
}