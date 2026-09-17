using Unity.VisualScripting;
using UnityEngine;

public class EnemView : MonoBehaviour
{
    public Vector2 Sight = Vector2.down;
    public float SightAngle = 60;
    public float SightRange = 5;
    public Transform rayPoint;
    [SerializeField] Transform target;

    void Awake()
    {
        target = GameObject.FindGameObjectsWithTag("Player")[0].transform;
        rayPoint = this.GameObject().transform;
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void FixedUpdate()
    {
        Vector2 targetPos = (target.position - rayPoint.position);
        float PosAngle = Vector2.Angle(targetPos, Sight);
        RaycastHit2D ray = Physics2D.Raycast(rayPoint.position, targetPos, SightRange);
        if(PosAngle < SightAngle / 2 && ray.collider != null && ray.collider.CompareTag("Player"))
        {
            melee_enem enem = this.GameObject().GetComponent<melee_enem>();
        }
    }
}
