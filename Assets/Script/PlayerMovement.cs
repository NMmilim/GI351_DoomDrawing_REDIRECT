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
        // Get the mouse position in world space
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.z = 0; // Set z to 0 for 2D
            // Calculate the direction from the player to the mouse position
            Vector3 direction = (mousePosition - transform.position).normalized;
            // Calculate the angle in degrees
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            // Rotate the player to face the mouse position
            transform.rotation = Quaternion.Euler(0, 0, angle - 90); // Subtract 90 if your sprite faces up
      if (Input.GetMouseButtonDown(0))
        {
            Instantiate(bullet, transform.position + transform.up*0.5f , transform.rotation);
        }
    }
    public void FixedUpdate()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        rb.linearVelocity = new Vector2(horizontal, vertical) * speed;
      

    }

}
