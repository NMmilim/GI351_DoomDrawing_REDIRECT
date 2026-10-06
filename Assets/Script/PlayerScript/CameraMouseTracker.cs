using UnityEngine;

public class CameraMouseTracker : MonoBehaviour
{
    [Header("Tracking Settings")]
    [Tooltip("The transform of the player to track. If left empty, it tries to find the GameObject tagged 'Player'.")]
    public Transform playerTransform;

    [Tooltip("How far the camera target can move away from the player towards the mouse.")]
    public float maxDistance = 4f;

    [Tooltip("How much influence the mouse has. 0 = purely on player, 1 = exactly on mouse (clamped by maxDistance).")]
    [Range(0f, 1f)]
    public float mouseWeight = 0.4f;

    [Tooltip("How smoothly the target moves to the desired position. 0 = instant.")]
    public float smoothSpeed = 15f;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        
        if (playerTransform == null)
        {
            // Try to find a GameObject tagged "Player"
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("CameraMouseTracker: Player not found! Please assign it in the Inspector.");
            }
        }
    }

    private void LateUpdate()
    {
        if (playerTransform == null || mainCamera == null) return;

        // 1. Get mouse position in world space
        Vector3 mouseScreenPos = Input.mousePosition;
        // Set Z to the distance between camera and player so ScreenToWorldPoint works accurately in 3D/2D
        mouseScreenPos.z = Mathf.Abs(mainCamera.transform.position.z - playerTransform.position.z); 
        
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = playerTransform.position.z; // lock it to the player's Z plane (2D)

        // 2. Calculate distance and direction from player to mouse
        Vector3 offset = mouseWorldPos - playerTransform.position;

        // 3. Apply the weight (e.g. 0.5 means it stays exactly halfway between player and mouse)
        Vector3 targetOffset = offset * mouseWeight;

        // 4. Clamp the maximum distance it can travel away from the player
        if (targetOffset.magnitude > maxDistance)
        {
            targetOffset = targetOffset.normalized * maxDistance;
        }

        // 5. Set the position of this GameObject
        Vector3 desiredPosition = playerTransform.position + targetOffset;

        if (smoothSpeed > 0f)
        {
            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * smoothSpeed);
        }
        else
        {
            transform.position = desiredPosition;
        }
    }
}
