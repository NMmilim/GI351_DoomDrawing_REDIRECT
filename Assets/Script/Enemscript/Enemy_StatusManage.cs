using UnityEngine;

public class Enemy_StatusManage : MonoBehaviour
{
    public static Enemy_StatusManage Instance;
    [Header("STATUS")]
    public int maxhp;
    public int hp;
    public int armor;
    public int speed;
    [Header("COMBATSTATs")]
    public int damage;
    public float firerate;
    public int meleedamage;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
