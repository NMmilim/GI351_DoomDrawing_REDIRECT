using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Player damage / heal entry point.
/// When HP hits 0 → PlayerManagement.MarkDead() → GameManager.TriggerGameOver()
/// (shows the Game Over screen and banks coins for the base upgrade shop).
/// Note: Player.prefab has both Player and PlayerMovement (which inherits Player);
/// either one can receive hits — all state lives in PlayerManagement.
/// </summary>
public class Player : MonoBehaviour
{

    public void RegisterHit(int amount)
    {
        PlayerManagement pm = PlayerManagement.Instance;
        if (pm == null || pm.IsDead || amount <= 0) return;

        pm.hp = Mathf.Max(0, pm.hp - amount);

        if (pm.hp <= 0)
        {
            Die();
           // SceneManager.LoadScene(1);
        }
    }
    public void RegisterHeal(int amount)
    {
        PlayerManagement pm = PlayerManagement.Instance;
        if (pm == null || pm.IsDead || amount <= 0) return;
        pm.hp = Mathf.Min(pm.maxhp, pm.hp + amount);
    }

    private void Die()
    {
        PlayerManagement.Instance.MarkDead();

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (GameManager.Instance != null)
            GameManager.Instance.TriggerGameOver();
        else
        {
            Debug.LogWarning("[Player] Died but no GameManager in scene — pausing instead.");
            Time.timeScale = 0;
        }
    }
   
}
    
   
