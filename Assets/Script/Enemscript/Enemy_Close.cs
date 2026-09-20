

using Unity.VisualScripting;
using UnityEngine;

public class melee_enem : Enemy_Range
{

    public float movespeed;
    public Transform player;
    
  //  public Animator MeleeAnim;
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        
        
    }
    void Update()
    {
  




    }
}
