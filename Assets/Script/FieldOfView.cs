
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldOfView : MonoBehaviour
{
    public float radius = 5f;
    [Range(1, 360)] public float angle = 90f;
    public int segments = 50;
    public Transform player;
    public LayerMask obstructionMask;
    public bool CanSeePlayer { get; private set; }

    private Mesh mesh;

    void Awake()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    void LateUpdate()
    {
        DrawVisionCone();
        DetectPlayer();
        transform.rotation = transform.parent.rotation;
    }

    void DrawVisionCone()
    {
        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero; // center of cone

        float angleStep = angle / segments;
        float currentAngle = -angle / 2;

        for (int i = 0; i <= segments; i++)
        {
            Vector3 dir = Quaternion.Euler(0, 0, currentAngle) * transform.up;
            vertices[i + 1] = dir * radius;

            if (i < segments)
            {
                int start = i * 3;
                triangles[start] = 0;
                triangles[start + 1] = i + 1;
                triangles[start + 2] = i + 2;
            }

            currentAngle += angleStep;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
    }

    void DetectPlayer()
    {
        if (player == null) return;

        Vector2 dirToPlayer = (player.position - transform.position).normalized;
        float angleToPlayer = Vector2.Angle(transform.up, dirToPlayer);

        if (angleToPlayer < angle / 2)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dirToPlayer, dist, obstructionMask);

            if (hit.collider == null) // no wall blocking
            {
                CanSeePlayer = true;
                Debug.Log("Enemy sees player!");
            }
            else
            {
                CanSeePlayer = false;
            }
        }
        else
        {
            CanSeePlayer = false;
        }
    }
}
