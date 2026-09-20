using UnityEngine;
public class Player : MonoBehaviour
{

    public void RegisterHit()
    {
        PlayerManagement.Instance.hitpoint++;

        if (PlayerManagement.Instance.hitpoint >= PlayerManagement.Instance.maxHitpoint)
        {
            Destroy(gameObject);
        }
    }

}
    
   
