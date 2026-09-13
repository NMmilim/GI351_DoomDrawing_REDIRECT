using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;

    public int CurrentHealth => currentHealth;

    public SceneLoaderGameOver gameOverManager;
    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log($"Player HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log($"Player Heal → {currentHealth}/{maxHealth}");
    }


    void Die()
    {
        Debug.Log("Player ตายแล้ว");

        gameObject.SetActive(false);

        if (gameOverManager != null)
        {
            gameOverManager.ShowGameOver();
        }
    }

}