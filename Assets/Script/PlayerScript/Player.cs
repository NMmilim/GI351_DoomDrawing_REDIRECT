using UnityEngine;
using UnityEngine.SceneManagement;
public class Player : MonoBehaviour
{
   

    public void RegisterHit()
    {
        PlayerManagement.Instance.hp -= Enemy_StatusManage.Instance.damage;

        if (PlayerManagement.Instance.hp == 0)
        {
            Time.timeScale = 0;
           // SceneManager.LoadScene(1);
        }
    }

}
    
   
