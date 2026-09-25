using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{

    public void RegisterHit(int amount)
    {
        PlayerManagement.Instance.hp -= amount;

        if (PlayerManagement.Instance.hp <= 0)
        {
            Time.timeScale = 0;
           // SceneManager.LoadScene(1);
        }
    }
    public void RegisterHeal(int amount)
    {
        PlayerManagement.Instance.hp += amount;
    }

   
}
    
   
