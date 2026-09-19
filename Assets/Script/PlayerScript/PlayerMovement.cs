using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class PlayerMovement : Player
{
    
    public GameObject bullet;
    //public Animator _playeranim;
    
    public Rigidbody2D rb;
    public void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
    }
    public void Update()
    {
        if (Time.timeScale == 0f) return;
      

        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0; // Set z to 0 for 2D

        Vector3 direction = (mousePosition - transform.position).normalized;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0, 0, angle - 90);
    }
         public void RegisterHit()
    {
        PlayerManagement.Instance.hitpoint++;

        if (PlayerManagement.Instance.hitpoint >= PlayerManagement.Instance.maxHitpoint)
        {
            Destroy(gameObject);
        }
    }

        public void FixedUpdate()
    {
        if (Time.timeScale == 0f) return;
        float horizontal = Input.GetAxis("Horizontal");
    float vertical = Input.GetAxis("Vertical");
    //_playeranim.SetFloat("horizontal", horizontal);
    //_playeranim.SetFloat("vertical", vertical);
    rb.linearVelocity = new Vector2(horizontal, vertical) * PlayerManagement.Instance.speed;


    }

}





