using UnityEngine;
public class Player : MonoBehaviour
{

    public void RegisterHit()
    {
        PlayerManagement.Instance.hp -= PlayerManagement.Instance.dmg;

        if (PlayerManagement.Instance.hp == 0)
        {
            Destroy(gameObject);
        }
    }

}
    
   
