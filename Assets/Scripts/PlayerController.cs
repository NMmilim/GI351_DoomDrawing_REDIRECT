using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;

    public Camera cam;

    void Update()
    {
        Move();
        RotateToMouse();
    }

    void Move()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 move = new Vector3(h, 0, v).normalized;

        transform.Translate(move * moveSpeed * Time.deltaTime, Space.World);
    }

    void RotateToMouse()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        float rayDistance;

        if (groundPlane.Raycast(ray, out rayDistance))
        {
            Vector3 point = ray.GetPoint(rayDistance);

            Vector3 lookDir = point - transform.position;
            lookDir.y = 0;

            if (lookDir != Vector3.zero)
            {
                transform.forward = lookDir.normalized;
            }
        }
    }
}