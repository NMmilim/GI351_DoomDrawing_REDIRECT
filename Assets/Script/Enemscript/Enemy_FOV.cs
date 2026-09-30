using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 2D Enemy Line-of-Sight — "Flashlight" visual and fire-zone gate.
///
/// • Cosmetic: renders a narrow transparent pale-yellow cone (line-of-fire indicator)
///   that follows wherever the enemy body is rotated.
/// • A pulsing tapering laser centre-line shows the exact aim direction.
/// • Enemy_Range drives all AI logic and body rotation independently.
///   The enemy ALWAYS knows where the player is — it never stops tracking them.
/// • IsInFireCone(pos): returns true when targetPos is inside the narrow cone angle
///   AND has an unobstructed raycast. Enemy_Range uses this as the sole fire gate.
///   (Not detection — the enemy aims at the player first, cone just validates the shot.)
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class EnemyFOV : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  Inspector – References
    // ─────────────────────────────────────────────────────────────

    [Header("References")]
    [Tooltip("The enemy body transform that owns the rotation. " +
             "Auto-resolved to parent if left empty.")]
    public Transform enemyBody;

    // ─────────────────────────────────────────────────────────────
    //  Inspector – LOS Shape
    // ─────────────────────────────────────────────────────────────

    [Header("LOS Shape")]
    [Tooltip("Max aim / fire range in world units.")]
    public float viewRadius = 8f;

    [Tooltip("Narrow cone angle in degrees. Keep 10-25 for a tight flashlight feel.")]
    [Range(1f, 60f)]
    public float viewAngle = 15f;

    [Tooltip("Triangle segments for the cone arc. 16 is plenty for a narrow beam.")]
    [Range(4, 64)]
    public int meshResolution = 16;

    // ─────────────────────────────────────────────────────────────
    //  Inspector – Line-of-Fire Check
    // ─────────────────────────────────────────────────────────────

    [Header("Line-of-Fire Check")]
    [Tooltip("Layers that block bullets (walls, obstacles). Used by HasClearLineOfFire().")]
    public LayerMask obstacleMask;

    // ─────────────────────────────────────────────────────────────
    //  Inspector – Flashlight Visuals
    // ─────────────────────────────────────────────────────────────

    [Header("Flashlight Visuals")]
    [Tooltip("Cone fill when line of fire is clear — brighter.")]
    public Color activeBeamColor = new Color(1f, 0.82f, 0.4f, 0.30f);

    [Tooltip("Cone fill when line of fire is blocked — dimmer.")]
    public Color blockedBeamColor = new Color(1f, 0.96f, 0.75f, 0.12f);

    [Tooltip("Speed the cone colour blends between states.")]
    public float colorBlendSpeed = 8f;

    [Tooltip("Laser centre-line colour.")]
    public Color laserColor = new Color(1f, 0.9f, 0.5f, 0.9f);

    [Tooltip("Width of the laser centre-line (world units).")]
    public float laserWidth = 0.03f;

    [Tooltip("Pulse speed for the laser alpha (0 = no pulse).")]
    public float laserPulseSpeed = 3f;

    [Tooltip("Sorting layer for the cone mesh.")]
    public string coneSortingLayer = "Default";

    [Tooltip("Sorting order within the layer.")]
    public int coneSortingOrder = -1;

    // ─────────────────────────────────────────────────────────────
    //  Public API  (read by Enemy_Range)
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if <paramref name="targetPos"/> is within the narrow cone angle
    /// AND has an unobstructed straight raycast from this transform.
    ///
    /// This is the FIRE GATE used by Enemy_Range — not a detection method.
    /// The enemy already knows the player's position; this only decides if a shot
    /// is currently valid (player is inside the beam and no wall is in the way).
    /// </summary>
    public bool IsInFireCone(Vector3 targetPos)
    {
        Vector2 origin = transform.position;
        Vector2 dir    = ((Vector2)targetPos - origin).normalized;
        float   dist   = Vector2.Distance(origin, targetPos);

        // 1. Distance check
        if (dist > viewRadius) return false;

        // 2. Cone angle check — is the target within the narrow beam?
        if (Vector2.Angle(ForwardDir(), dir) > viewAngle * 0.5f) return false;

        // 3. Obstacle (wall) check
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, dist, obstacleMask);
        return hit.collider == null;
    }

    /// <summary>
    /// Simple unobstructed raycast with no cone restriction.
    /// Kept as a fallback if no EnemyFOV is present on the enemy.
    /// </summary>
    public bool HasClearLineOfFire(Vector3 targetPos)
    {
        Vector2 origin = transform.position;
        Vector2 dir    = ((Vector2)targetPos - origin).normalized;
        float   dist   = Vector2.Distance(origin, targetPos);

        RaycastHit2D hit = Physics2D.Raycast(origin, dir, dist, obstacleMask);
        return hit.collider == null;
    }

    // ─────────────────────────────────────────────────────────────
    //  Private
    // ─────────────────────────────────────────────────────────────

    private Transform _enemyBody;

    // Cone mesh
    private Mesh         _coneMesh;
    private MeshFilter   _meshFilter;
    private MeshRenderer _meshRenderer;
    private Material     _coneMaterial;
    private Color        _currentColor;

    // Laser line
    private LineRenderer _laser;
    private float        _laserPulseT;

    // ─────────────────────────────────────────────────────────────
    //  Unity Messages
    // ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        _enemyBody = (enemyBody != null) ? enemyBody
                   : (transform.parent != null ? transform.parent : transform);

        SetupConeMesh();
        SetupLaser();
    }

    private void Start()
    {
        _currentColor = blockedBeamColor;
    }

    private void Update()
    {
        UpdateVisuals();
    }

    // ─────────────────────────────────────────────────────────────
    //  Setup
    // ─────────────────────────────────────────────────────────────

    private void SetupConeMesh()
    {
        _meshFilter   = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();

        _coneMesh = new Mesh { name = "LOSBeamMesh" };
        _meshFilter.mesh = _coneMesh;

        _coneMaterial = new Material(Shader.Find("Sprites/Default")) { color = blockedBeamColor };
        _meshRenderer.material         = _coneMaterial;
        _meshRenderer.sortingLayerName = coneSortingLayer;
        _meshRenderer.sortingOrder     = coneSortingOrder;
    }

    private void SetupLaser()
    {
        GameObject go = new GameObject("LOS_LaserLine");
        go.transform.SetParent(transform, false);

        _laser = go.AddComponent<LineRenderer>();
        _laser.useWorldSpace     = true;
        _laser.positionCount     = 2;
        _laser.startWidth        = laserWidth;
        _laser.endWidth          = laserWidth * 0.1f;   // taper to a point at the tip
        _laser.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _laser.receiveShadows    = false;
        _laser.sortingLayerName  = coneSortingLayer;
        _laser.sortingOrder      = coneSortingOrder + 1;
        _laser.material          = new Material(Shader.Find("Sprites/Default")) { color = laserColor };
    }

    // ─────────────────────────────────────────────────────────────
    //  Visuals
    // ─────────────────────────────────────────────────────────────

    // Cached player transform for the visual colour update only (set by Enemy_Range)
    internal Transform _playerRef;

    private void UpdateVisuals()
    {
        // Brighten the cone when the player is actually inside the fire zone
        bool inZone = (_playerRef != null) && IsInFireCone(_playerRef.position);

        Color target  = inZone ? activeBeamColor : blockedBeamColor;
        _currentColor = Color.Lerp(_currentColor, target, Time.deltaTime * colorBlendSpeed);
        _coneMaterial.color = _currentColor;

        BuildConeMesh();

        // Pulse laser
        _laserPulseT += Time.deltaTime * laserPulseSpeed;
        float pulse = 0.55f + 0.45f * Mathf.Sin(_laserPulseT);
        Color lc = laserColor;
        lc.a *= pulse;
        _laser.material.color = lc;

        Vector3 origin   = transform.position;
        Vector3 tipWorld = origin + (Vector3)(ForwardDir() * viewRadius);
        _laser.SetPosition(0, origin);
        _laser.SetPosition(1, tipWorld);
    }

    private void BuildConeMesh()
    {
        int vertCount = meshResolution + 2;

        Vector3[] verts = new Vector3[vertCount];
        Vector2[] uvs   = new Vector2[vertCount];
        int[]     tris  = new int[meshResolution * 3];

        verts[0] = Vector3.zero;
        uvs[0]   = new Vector2(0.5f, 0.5f);

        float halfAngle = viewAngle * 0.5f;
        float step      = viewAngle / meshResolution;

        for (int i = 0; i <= meshResolution; i++)
        {
            float deg = -halfAngle + step * i;
            float rad = deg * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
            verts[i + 1] = dir * viewRadius;
            uvs[i + 1]   = new Vector2(dir.x * 0.5f + 0.5f, dir.y * 0.5f + 0.5f);
        }

        for (int i = 0; i < meshResolution; i++)
        {
            int b = i * 3;
            tris[b]     = 0;
            tris[b + 1] = i + 1;
            tris[b + 2] = i + 2;
        }

        _coneMesh.Clear();
        _coneMesh.vertices  = verts;
        _coneMesh.uv        = uvs;
        _coneMesh.triangles = tris;
        _coneMesh.RecalculateNormals();
    }

    // ─────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────

    private Vector2 ForwardDir() =>
        _enemyBody != null ? (Vector2)_enemyBody.right : (Vector2)transform.right;

    private static Vector2 AngleToDir(float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    // ─────────────────────────────────────────────────────────────
    //  Scene Gizmos (editor only)
    // ─────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Transform body   = (Application.isPlaying && _enemyBody != null)
                           ? _enemyBody
                           : (transform.parent != null ? transform.parent : transform);
        Vector3 origin   = body.position;
        float   fwdAngle = body.eulerAngles.z;

        // Flashlight arc outline (pale yellow)
        Gizmos.color = new Color(1f, 0.96f, 0.7f, 0.6f);
        DrawGizmoArc(origin, fwdAngle, viewRadius, viewAngle, 24);
        Gizmos.DrawLine(origin, origin + (Vector3)AngleToDir(fwdAngle - viewAngle * 0.5f) * viewRadius);
        Gizmos.DrawLine(origin, origin + (Vector3)AngleToDir(fwdAngle + viewAngle * 0.5f) * viewRadius);

        // Centre laser line
        Gizmos.color = new Color(1f, 0.9f, 0.4f, 0.9f);
        Gizmos.DrawLine(origin, origin + (Vector3)AngleToDir(fwdAngle) * viewRadius);
    }

    private static void DrawGizmoArc(Vector3 center, float fwdAngle,
                                      float radius, float totalAngle, int segs)
    {
        float step = totalAngle / segs;
        for (int i = 0; i < segs; i++)
        {
            Vector3 a = center + (Vector3)AngleToDir(fwdAngle - totalAngle * 0.5f + step * i)       * radius;
            Vector3 b = center + (Vector3)AngleToDir(fwdAngle - totalAngle * 0.5f + step * (i + 1)) * radius;
            Gizmos.DrawLine(a, b);
        }
    }
#endif
}
