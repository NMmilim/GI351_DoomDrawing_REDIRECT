

using Unity.VisualScripting;
using UnityEngine;

public class Enemy_Close : Enemy_Range
{

    private void OnCollisionEnter2D(Collision2D collision)
    {
        collision.gameObject.GetComponent<Player>().RegisterHit();
    }


}
