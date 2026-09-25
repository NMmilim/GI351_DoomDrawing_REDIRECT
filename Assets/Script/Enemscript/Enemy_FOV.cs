using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 2D Enemy Field of View (FOV) System
/// • Attach this to a CHILD empty object inside your enemy.
/// • Cone-shaped vision: Radius = 5, Angle = 90 degrees.
/// • Renders a LIVE cone mesh visible during gameplay (and in the Scene view).
/// • Cone turns red when the player is detected.
/// • The PARENT enemy body rotates to face/track the player.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class EnemyFOV : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  Inspector – References
    // ─────────────────────────────────────────────────────────────

    [Header("References")]
    [Tooltip("The enemy transform to rotate toward the player. " +
             "Leave empty to auto-detect (uses parent if this script is on a child, " +
             "or this GameObject if placed directly on the enemy).")]
    public Transform enemyBody;

    // ─────────────────────────────────────────────────────────────
    //  Inspector – FOV
    // ─────────────────────────────────────────────────────────────

    [Header("FOV Shape")]
    [Tooltip("Cone length in world units.")]
    public float viewRadius = 5f;

    [Tooltip("Full cone angle in degrees (split ±half on each side of forward).")]
    [Range(1f, 360f)]
    public float viewAngle = 90f;

    [Tooltip("Number of triangle segments that make up the cone arc. Higher = smoother.")]
    [Range(4, 128)]
    public int meshResolution = 32;

    // ─────────────────────────────────────────────────────────────
    //  Inspector – Detection
    // ─────────────────────────────────────────────────────────────

    [Header("Detection")]
    [Tooltip("Layer(s) the player occupies.")]
    public LayerMask playerMask;

    [Tooltip("Layer(s) that block line-of-sight (walls, obstacles).")]
    public LayerMask obstacleMask;

    [Tooltip("Seconds between each full detection sweep (lower = more responsive).")]
    public float detectionRate = 0.05f;

    // ─────────────────────────────────────────────────────────────
    //  Inspector – Tracking
    // ─────────────────────────────────────────────────────────────

    [Header("Tracking")]
    [Tooltip("Degrees/second the enemy rotates toward the player.")]
    public float trackingRotationSpeed = 180f;

    [Tooltip("Degrees/second the enemy rotates back to idle facing.")]
    public float returnRotationSpeed = 60f;

    // ─────────────────────────────────────────────────────────────
    //  Inspector – Visuals
    // ─────────────────────────────────────────────────────────────

    [Header("Cone Visuals")]
    [Tooltip("Cone colour when no player is detected.")]
    public Color idleColor = new Color(0.2f, 1f, 0.3f, 0.25f);

    [Tooltip("Cone colour when the player is detected.")]
    public Color alertColor = new Color(1f, 0.15f, 0.1f, 0.40f);

    [Tooltip("Speed at which the cone colour blends between idle/alert.")]
    public float colorBlendSpeed = 6f;

    [Tooltip("Show the two edge lines of the cone.")]
    public bool showEdgeLines = true;

    [Tooltip("Width of the edge lines (world units).")]
    public float edgeLineWidth = 0.04f;

    [Tooltip("Colour of the edge lines.")]
    public Color edgeLineColor = new Color(1f, 1f, 1f, 0.6f);

    [Tooltip("Sorting layer name for the cone mesh (must exist in Project Settings).")]
    public string coneSortingLayer = "Default";

    [Tooltip("Sorting order within the layer (lower = behind sprites).")]
    public int coneSortingOrder = -1;

    // ─────────────────────────────────────────────────────────────
    //  Inspector – Scene Gizmos
    // ─────────────────────────────────────────────────────────────

    [Header("Scene Gizmos")]
    public bool showGizmos = true;

    // ─────────────────────────────────────────────────────────────
    //  Public State
    // ─────────────────────────────────────────────────────────────

    /// <summary>True while player is inside cone AND has unobstructed line-of-sight.</summary>
    public bool PlayerInSight { get; private set; }

    /// <summary>The detected player's transform, or null.</summary>
    public Transform DetectedPlayer { get; private set; }

    // ─────────────────────────────────────────────────────────────
    //  Private
    // ─────────────────────────────────────────────────────────────

    // Resolved enemy body transform (the one that actually rotates)
    private Transform _enemyBody;

    // Rotation
    private float _targetAngle;
    private float _patrolAngle;

    // Detection timer
    private float _detectionTimer;

    // Runtime mesh
    private Mesh _coneMesh;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Material _coneMaterial;

    // Edge LineRenderers (left edge, right edge)
    private LineRenderer _leftEdge;
    private LineRenderer _rightEdge;

    // Current blended colour
    private Color _currentColor;

    // ─────────────────────────────────────────────────────────────
    //  Unity Messages
    // ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Auto-resolve the enemy body: use the assigned field, else parent, else self.
        if (enemyBody != null)
            _enemyBody = enemyBody;
        else if (transform.parent != null)
            _enemyBody = transform.parent;
        else
            _enemyBody = transform;

        SetupConeMesh();
        SetupEdgeLines();
    }

    private void Start()
    {
        _patrolAngle = _enemyBody.eulerAngles.z;
        _targetAngle = _patrolAngle;
        _currentColor = idleColor;
    }

    private void Update()
    {
        RunDetectionTimer();
        HandleRotation();
        UpdateConeVisual();
    }

    // ─────────────────────────────────────────────────────────────
    //  Visual Setup
    // ─────────────────────────────────────────────────────────────

    private void SetupConeMesh()
    {
        // Use this GameObject's own MeshFilter / MeshRenderer
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();

        _coneMesh = new Mesh { name = "FOVConeMesh" };
        _meshFilter.mesh = _coneMesh;

        // Unlit transparent material — no extra assets needed
        _coneMaterial = new Material(Shader.Find("Sprites/Default"))
        {
            color = idleColor
        };
        _meshRenderer.material = _coneMaterial;
        _meshRenderer.sortingLayerName = coneSortingLayer;
        _meshRenderer.sortingOrder = coneSortingOrder;
    }

    private void SetupEdgeLines()
    {
        if (!showEdgeLines) return;

        _leftEdge = CreateEdgeLine("FOV_LeftEdge");
        _rightEdge = CreateEdgeLine("FOV_RightEdge");
    }

    private LineRenderer CreateEdgeLine(string goName)
    {
        GameObject go = new GameObject(goName);
        go.transform.SetParent(transform, false);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.startWidth = edgeLineWidth;
        lr.endWidth = edgeLineWidth;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.sortingLayerName = coneSortingLayer;
        lr.sortingOrder = coneSortingOrder + 1;   // draw on top of the filled cone

        Material lrMat = new Material(Shader.Find("Sprites/Default")) { color = edgeLineColor };
        lr.material = lrMat;
        return lr;
    }

    // ─────────────────────────────────────────────────────────────
    //  Detection
    // ─────────────────────────────────────────────────────────────

    private void RunDetectionTimer()
    {
        _detectionTimer -= Time.deltaTime;
        if (_detectionTimer > 0f) return;

        _detectionTimer = detectionRate;
        CheckForPlayer();
    }

    private void CheckForPlayer()
    {
        // 1. Radius + layer check
        Collider2D hit = Physics2D.OverlapCircle(transform.position, viewRadius, playerMask);
        if (hit == null) { LosePlayer(); return; }

        Transform player = hit.transform;
        Vector2 dirToPlayer = (player.position - transform.position).normalized;
        float distToPlayer = Vector2.Distance(transform.position, player.position);

        // 2. Cone angle check
        if (Vector2.Angle(ForwardDirection(), dirToPlayer) > viewAngle * 0.5f)
        {
            LosePlayer(); return;
        }

        // 3. Line-of-sight check
        RaycastHit2D los = Physics2D.Raycast(
            transform.position, dirToPlayer, distToPlayer, obstacleMask);

        if (los.collider != null) { LosePlayer(); return; }

        // All conditions passed — player is detected
        PlayerInSight = true;
        DetectedPlayer = player;
        _targetAngle = DirectionToAngle(dirToPlayer);
    }


    private void LosePlayer()
    {
        if (!PlayerInSight) return;

        PlayerInSight = false;
        DetectedPlayer = null;
        _targetAngle = _patrolAngle;
    }

    // ─────────────────────────────────────────────────────────────
    //  Rotation
    // ─────────────────────────────────────────────────────────────

    private void HandleRotation()
    {
        if (PlayerInSight && DetectedPlayer != null)
        {
            // Direction from the enemy body toward the player
            Vector2 dir = (DetectedPlayer.position - _enemyBody.position).normalized;
            _targetAngle = DirectionToAngle(dir);
        }

        float speed = PlayerInSight ? trackingRotationSpeed : returnRotationSpeed;
        float newAngle = Mathf.MoveTowardsAngle(
            _enemyBody.eulerAngles.z, _targetAngle, speed * Time.deltaTime);

        // Rotate the PARENT enemy body, not the FOV child
        _enemyBody.rotation = Quaternion.Euler(0f, 0f, newAngle);
    }

    // ─────────────────────────────────────────────────────────────
    //  Runtime Cone Visual Update
    // ─────────────────────────────────────────────────────────────

    private void UpdateConeVisual()
    {
        // ── Blend colour ──
        Color targetColor = PlayerInSight ? alertColor : idleColor;
        _currentColor = Color.Lerp(_currentColor, targetColor, Time.deltaTime * colorBlendSpeed);
        _coneMaterial.color = _currentColor;

        // ── Rebuild mesh in LOCAL space (so it follows the transform) ──
        BuildConeMesh();

        // ── Edge lines ──
        if (showEdgeLines && _leftEdge != null && _rightEdge != null)
        {
            // The cone spans ± half-angle around the LOCAL forward (+X = angle 0 in local space)
            float halfAngle = viewAngle * 0.5f;

            // Local-space edge directions, then converted to world space
            Vector3 leftDir = LocalAngleToWorldDir(-halfAngle);
            Vector3 rightDir = LocalAngleToWorldDir(+halfAngle);

            Vector3 origin = transform.position;
            _leftEdge.SetPosition(0, origin);
            _leftEdge.SetPosition(1, origin + leftDir * viewRadius);

            _rightEdge.SetPosition(0, origin);
            _rightEdge.SetPosition(1, origin + rightDir * viewRadius);

            Color lc = edgeLineColor;
            lc.a = _currentColor.a * 2.5f;   // keep edges more opaque than fill
            _leftEdge.material.color = lc;
            _rightEdge.material.color = lc;
        }
    }

    /// <summary>
    /// Rebuilds the cone mesh in LOCAL space.
    /// The cone always points along the local +X axis so it automatically
    /// follows any rotation applied to the transform.
    /// </summary>
    private void BuildConeMesh()
    {
        int vertCount = meshResolution + 2;   // origin + arc vertices

        Vector3[] verts = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        int[] tris = new int[meshResolution * 3];

        // Vertex 0 = cone origin (local centre)
        verts[0] = Vector3.zero;
        uvs[0] = new Vector2(0.5f, 0.5f);

        float halfAngle = viewAngle * 0.5f;
        float stepAngle = viewAngle / meshResolution;

        for (int i = 0; i <= meshResolution; i++)
        {
            // Angle relative to local forward (+X), going from -halfAngle to +halfAngle
            float angleDeg = -halfAngle + stepAngle * i;
            float angleRad = angleDeg * Mathf.Deg2Rad;

            Vector3 dir = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f);
            verts[i + 1] = dir * viewRadius;
            uvs[i + 1] = new Vector2(dir.x * 0.5f + 0.5f, dir.y * 0.5f + 0.5f);
        }

        for (int i = 0; i < meshResolution; i++)
        {
            int b = i * 3;
            tris[b] = 0;
            tris[b + 1] = i + 1;
            tris[b + 2] = i + 2;
        }

        _coneMesh.Clear();
        _coneMesh.vertices = verts;
        _coneMesh.uv = uvs;
        _coneMesh.triangles = tris;
        _coneMesh.RecalculateNormals();
    }

    // ─────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// The enemy's forward direction in world space.
    /// Reads from the enemy BODY so the cone angle matches where the body faces.
    /// Default = _enemyBody.right (+X). Change to _enemyBody.up if sprite faces up.
    /// </summary>
    private Vector2 ForwardDirection() => _enemyBody != null ? (Vector2)_enemyBody.right : (Vector2)transform.right;

    /// <summary>Returns a world-space direction offset by localOffsetDeg from local forward.</summary>
    private Vector3 LocalAngleToWorldDir(float localOffsetDeg)
    {
        // Offset from the enemy BODY's current facing angle
        float bodyAngle = _enemyBody != null ? _enemyBody.eulerAngles.z : transform.eulerAngles.z;
        float worldAngle = bodyAngle + localOffsetDeg;
        float rad = worldAngle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
    }

    private static float DirectionToAngle(Vector2 dir)
        => Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

    private static Vector2 AngleToDirection(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    // ─────────────────────────────────────────────────────────────
    //  Scene-View Gizmos (editor overlay on top of the runtime mesh)
    // ─────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // In the Editor _enemyBody may not be resolved yet — fall back gracefully
        Transform body = (Application.isPlaying && _enemyBody != null) ? _enemyBody
                         : (transform.parent != null ? transform.parent : transform);
        Vector3 origin = body.position;
        float fwdAngle = body.eulerAngles.z;
        bool alert = Application.isPlaying && PlayerInSight;

        // Outline arc
        Gizmos.color = alert ? Color.red : Color.green;
        DrawGizmoArc(origin, fwdAngle, viewRadius, viewAngle, 32);
        Gizmos.DrawLine(origin, origin + (Vector3)AngleToDirection(fwdAngle - viewAngle * 0.5f) * viewRadius);
        Gizmos.DrawLine(origin, origin + (Vector3)AngleToDirection(fwdAngle + viewAngle * 0.5f) * viewRadius);

        // Detection range circle
        Gizmos.color = new Color(1f, 1f, 1f, 0.12f);
        DrawGizmoCircle(origin, viewRadius, 64);

        // Line to player when detected
        if (Application.isPlaying && alert && DetectedPlayer != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, DetectedPlayer.position);
        }
    }

    private static void DrawGizmoArc(Vector3 center, float fwdAngle,
                                      float radius, float totalAngle, int segs)
    {
        float step = totalAngle / segs;
        for (int i = 0; i < segs; i++)
        {
            Vector3 a = center + (Vector3)AngleToDirection(fwdAngle - totalAngle * 0.5f + step * i) * radius;
            Vector3 b = center + (Vector3)AngleToDirection(fwdAngle - totalAngle * 0.5f + step * (i + 1)) * radius;
            Gizmos.DrawLine(a, b);
        }
    }

    private static void DrawGizmoCircle(Vector3 center, float radius, int segs)
    {
        float step = 360f / segs;
        for (int i = 0; i < segs; i++)
        {
            Vector3 a = center + (Vector3)AngleToDirection(step * i) * radius;
            Vector3 b = center + (Vector3)AngleToDirection(step * (i + 1)) * radius;
            Gizmos.DrawLine(a, b);
        }
    }
#endif
}
