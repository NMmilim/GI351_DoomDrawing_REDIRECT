using Unity.VisualScripting;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int maxHit = 1;
    private int currentHit=0;

    public void RegisterHit()
    {
        currentHit++;

        if (currentHit >= maxHit)
        {
            Destroy(gameObject);
        }
    }
}
