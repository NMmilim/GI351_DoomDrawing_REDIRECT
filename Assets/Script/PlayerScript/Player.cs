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
    public void MeleeRegisterHit()
    {
        PlayerManagement.Instance.hp -= Enemy_StatusManage.Instance.meleedamage;

        if (PlayerManagement.Instance.hp <= 0)
        {
            gameObject.SetActive(false);
            // SceneManager.LoadScene(1);
        }
    }
}
    
   
