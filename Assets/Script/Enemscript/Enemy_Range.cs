using Unity.VisualScripting;
using UnityEngine;

public class Enemy_Range : MonoBehaviour
{
    public int maxHit = Enemy_StatusManage.Instance.maxhp;

    private int currentHit = Enemy_StatusManage.Instance.hp;
   
    public void RegisterHit()
    {
        currentHit++;

        if (currentHit >= maxHit)
        {
            Destroy(gameObject);
        }
    }
}
