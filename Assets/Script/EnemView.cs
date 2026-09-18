using Unity.VisualScripting;
using UnityEngine;

public class EnemView : MonoBehaviour
{
    public Vector2 Sight = Vector2.down;
    public float SightAngle = 60;
    public float SightRange = 5;
    public Transform rayPoint;
    private Transform player;
    private float movespeed = 1;
    public bool PlayerDetect { get; private set; }
    public bool IsChasing { get; private set; }


    [SerializeField] Transform target;


    void Awake()
    {
        target = GameObject.FindGameObjectsWithTag("Player")[0].transform;
        rayPoint = this.GameObject().transform;
        player = GameObject.FindGameObjectWithTag("Player").transform;

    }

    void Start()
    {

    }



    void FixedUpdate()
    {
        
        Vector2 targetPos = (target.position - rayPoint.position);
        float PosAngle = Vector2.Angle(targetPos, Sight);
        RaycastHit2D ray = Physics2D.Raycast(rayPoint.position, targetPos, SightRange);
        if (PosAngle < SightAngle / 2 && ray.collider != null)
        {
            if (ray.collider.tag != "Player")
            {

                PlayerDetect = true;
                IsChasing = true;
                Debug.Log("Alert!");
                
                

            }



        }
        Debug.DrawRay(rayPoint.position, Sight.normalized * SightRange, Color.yellow);
    }
}