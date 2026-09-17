using UnityEngine;

public class Shooting : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firepos;
    
    
  
    // Update is called once per frame
    void Update()
    {
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
