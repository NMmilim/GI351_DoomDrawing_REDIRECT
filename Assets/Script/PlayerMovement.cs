using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class PlayerMovement : Player
{
    public GameObject bullet;
   
    public float speed;
    public Rigidbody2D rb;
    public void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        speed = 5;
    }
    public void Update()
    {
        
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.z = 0; // Set z to 0 for 2D
            
            Vector3 direction = (mousePosition - transform.position).normalized;
            
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            transform.rotation = Quaternion.Euler(0, 0, angle - 90); 
 
    }
    public void FixedUpdate()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        rb.linearVelocity = new Vector2(horizontal, vertical) * speed;
      

    }

}
