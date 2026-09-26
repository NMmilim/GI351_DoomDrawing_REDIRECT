using UnityEngine;

public class PlayerManagement : MonoBehaviour
{
    public static PlayerManagement Instance;
    [Header("Player Status")]
    public int hp;
    public int maxhp;
    
    public float armor;
    public float speed;
    [Header("Player Combat stats")]
    public int dmg;
    public float cooldown=1;
   public float nextFireTime=0;
    [Header("Player Upgrades")]
    public float UpgRicochet;
    public float Piercing;
    public float BulletSpeed;


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
