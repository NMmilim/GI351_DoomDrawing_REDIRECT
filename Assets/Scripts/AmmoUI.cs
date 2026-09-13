using UnityEngine;
using TMPro;

public class AmmoUI : MonoBehaviour
{
    public PlayerShooting playerShooting;
    public TextMeshProUGUI ammoText;

    void Update()
    {
        
        if (playerShooting.isReloading)
        {
            ammoText.color = Color.yellow;
            ammoText.text = "Reloading...";
        }

        
        else if (playerShooting.CurrentAmmo <= 0)
        {
            ammoText.color = Color.red;
            ammoText.text = "Out Of Ammo";
        }

        else
        {
            ammoText.color = Color.white;
            ammoText.text = playerShooting.CurrentAmmo + " / " + playerShooting.maxAmmo;
        }
    }
}