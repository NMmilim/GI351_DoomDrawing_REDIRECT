using UnityEngine;
using UnityEngine.SceneManagement;
public class Player : MonoBehaviour
{
   

    public void RegisterHit()
    {
        PlayerManagement.Instance.hp -= Enemy_StatusManage.Instance.damage;

        if (PlayerManagement.Instance.hp <= 0)
        {
            gameObject.SetActive(false);
           // SceneManager.LoadScene(1);
        }
    }
    public void ChangeHealth(int amount)
    {
        PlayerManagement.Instance.hp += amount;


    }
}
    
   
