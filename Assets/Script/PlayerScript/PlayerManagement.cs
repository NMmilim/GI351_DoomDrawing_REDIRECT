using UnityEngine;

public class PlayerManagement : MonoBehaviour
{
    public static PlayerManagement Instance;
    [Header("Player Status")]
    public int hitpoint;
    public int maxHitpoint;
    public float hitdmg;
    public float armor;
    public float speed;
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
