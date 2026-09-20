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

    }
    public void RegisterHit()
    {
        Debug.Log(hp);
        hp -= PlayerManagement.Instance.dmg;
        if (hp == 0)
        {
            Destroy(gameObject);
        }
    }
}
