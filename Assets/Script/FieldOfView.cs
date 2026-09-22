
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldOfView : MonoBehaviour
{
   public bool IsChasing;
    public Transform player;
    
    public void OnTriggerEnter2D(Collider2D collision)
    {
        
        
        if (collision.gameObject.tag == "Player") {IsChasing = true; }
    }
    public void OnTriggerExit2D(Collider2D collision)
    {
        
        if (collision.gameObject.tag == "Player") {IsChasing = false;}  
        
    }
}
