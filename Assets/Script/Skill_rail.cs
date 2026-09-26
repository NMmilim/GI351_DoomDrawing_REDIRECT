using UnityEngine;

public class Skill_ : Skill_auto
{
    public GameObject RailBullet;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha2)&&Time.time >= PlayerManagement.Instance.nextFireTime)
        {
            RailGun();
             PlayerManagement.Instance.nextFireTime = Time.time + (PlayerManagement.Instance.cooldown*3);
        }
        void RailGun()
        {
            RailBullet = Instantiate(bulletPrefab, firepos.position, firepos.rotation);
        }
    }
}
