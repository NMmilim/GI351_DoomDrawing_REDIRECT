using UnityEngine;

public class SmartCameraHeight : MonoBehaviour
{
    public Transform player;

    public float defaultHeight = 10f;
    public float minHeight = 3f;
    public float smoothSpeed = 5f;

    public LayerMask obstacleLayer;

    private float currentHeight;

    void Start()
    {
        currentHeight = defaultHeight;
    }

    void LateUpdate()
    {
        AdjustHeight();
        FollowPlayer();
    }

    void AdjustHeight()
    {
        Ray ray = new Ray(player.position, Vector3.up);
        RaycastHit hit;

        float targetHeight = defaultHeight;

        if (Physics.Raycast(ray, out hit, defaultHeight, obstacleLayer))
        {
            if (hit.collider.CompareTag("Glass"))
            {
                
                float glassHeight = hit.point.y + 2.5f; 
                targetHeight = glassHeight - player.position.y;
            }
            else
            {
                
                float distance = hit.distance - 0.5f;
                targetHeight = Mathf.Clamp(distance, minHeight, defaultHeight);
            }
        }

        currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * smoothSpeed);
    }

    void FollowPlayer()
    {
        Vector3 targetPos = player.position + new Vector3(0, currentHeight, 0);
        transform.position = targetPos;

        transform.LookAt(player);
    }
}
