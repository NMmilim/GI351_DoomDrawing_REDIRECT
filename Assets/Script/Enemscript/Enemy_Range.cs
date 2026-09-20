using Unity.VisualScripting;
using UnityEngine;

public class Enemy_Range : MonoBehaviour
{
    int hp;
    float firerate;
    public void Start()
    {
        hp = Enemy_StatusManage.Instance.hp;
        firerate = Enemy_StatusManage.Instance.firerate;
        Debug.Log(hp);

    }
    public void RegisterHit()
    {
        hp -= PlayerManagement.Instance.dmg;
        Debug.Log(hp);
        if (hp <= 0)
        {
            
            Destroy(gameObject);
        }
    }
}
