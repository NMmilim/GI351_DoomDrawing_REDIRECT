using Unity.VisualScripting;
using UnityEngine;

public class Shooting : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firepos;
   

   
   

       
        void Update()
        {
        if (Time.timeScale == 0f) return;
        if (Input.GetMouseButtonDown(0))
            {
                Shooting();
            }
            void Shooting()
            {
                GameObject bullet = Instantiate(bulletPrefab, firepos.position, firepos.rotation);
            }

        }
    
    
}
