using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 2D Enemy Line-of-Sight — visual cone and fire-zone gate.
///
/// • Renders a narrow transparent cone (line-of-fire indicator).
/// • IsInFireCone(pos): returns true when the target is inside the cone
///   AND has an unobstructed raycast. Used by Enemy_Range as its fire gate.
/// • The enemy AI drives all logic and rotation. This component is purely
///   visual + the fire-angle check.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class EnemyFOV : MonoBehaviour
{
    // -----------------------------------------------------------------
    //  Inspector – References
    // -----------------------------------------------------------------

    [Header("References")]
    [Tooltip("The enemy body transform. Auto-resolved to parent if left empty.")]
    public Transform enemyBody;

    // -----------------------------------------------------------------
    //  Inspector – LOS Shape
    // -----------------------------------------------------------------

    [Header("LOS Shape")]
    [Tooltip("Max fire range in world units.")]
    public float viewRadius = 8f;

    [Tooltip("Narrow cone angle in degrees.")]
    [Range(1f, 60f)]
    public float viewAngle = 15f;

    [Tooltip("Triangle segments for the cone arc.")]
    [Range(4, 64)]
    public int meshResolution = 16;

    // -----------------------------------------------------------------
    //  Inspector – Line-of-Fire Check
    // -----------------------------------------------------------------

    [Header("Line-of-Fire Check")]
    [Tooltip("Layers that block bullets (walls, obstacles).")]
    public LayerMask obstacleMask;

    // -----------------------------------------------------------------
    //  Inspector – Visuals
    // -----------------------------------------------------------------

    [Header("Flashlight Visuals")]
    public Color activeBeamColor  = new Color(1f, 0.82f, 0.4f, 0.30f);
    public Color blockedBeamColor = new Color(1f, 0.96f, 0.75f, 0.12f);
    public float colorBlendSpeed  = 8f;

    public Color laserColor      = new Color(1f, 0.9f, 0.5f, 0.9f);
    public float laserWidth      = 0.03f;
    public float laserPulseSpeed = 3f;
    public bool  showLaser       = false;

    public string coneSortingLayer = "Default";
    public int    coneSortingOrder = -1;

    // -----------------------------------------------------------------
    //  Public API
    // -----------------------------------------------------------------

    /// <summary>
    /// Returns true if targetPos is within the cone angle AND has an
    /// unobstructed straight raycast. Used by Enemy_Range as the fire gate.
    /// </summary>
    public bool IsInFireCone(Vector3 targetPos)
    {
        Vector2 origin = transform.position;
        Vector2 dir    = ((Vector2)targetPos - origin).normalized;
        float   dist   = Vector2.Distance(origin, targetPos);

        if (dist > viewRadius) return false;
        if (Vector2.Angle(ForwardDir(), dir) > viewAngle * 0.5f) return false;

        RaycastHit2D hit = Physics2D.Raycast(origin, dir, dist, obstacleMask);
        return hit.collider == null;
    }

    // -----------------------------------------------------------------
    //  Internal (set by Enemy_Range)
    // -----------------------------------------------------------------

    internal Transform _playerRef;

    // -----------------------------------------------------------------
    //  Private
    // -----------------------------------------------------------------

    private Transform    _enemyBody;
    private Mesh         _coneMesh;
    private MeshFilter   _meshFilter;
    private MeshRenderer _meshRenderer;
    private Material     _coneMaterial;
    private Color        _currentColor;
    private LineRenderer _laser;
    private float        _laserPulseT;

    // -----------------------------------------------------------------
    //  Unity Messages
    // -----------------------------------------------------------------

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

    // -----------------------------------------------------------------
    //  Setup
    // -----------------------------------------------------------------

    private void SetupConeMesh()
    {
        _meshFilter   = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();
        _coneMesh     = new Mesh { name = "LOSBeamMesh" };
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
        _laser.endWidth          = laserWidth * 0.1f;
        _laser.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _laser.receiveShadows    = false;
        _laser.sortingLayerName  = coneSortingLayer;
        _laser.sortingOrder      = coneSortingOrder + 1;
        _laser.material          = new Material(Shader.Find("Sprites/Default")) { color = laserColor };
    }

    // -----------------------------------------------------------------
    //  Visuals
    // -----------------------------------------------------------------

    private void UpdateVisuals()
    {
        bool inZone   = (_playerRef != null) && IsInFireCone(_playerRef.position);
        Color target  = inZone ? activeBeamColor : blockedBeamColor;
        _currentColor = Color.Lerp(_currentColor, target, Time.deltaTime * colorBlendSpeed);
        _coneMaterial.color = _currentColor;

        BuildConeMesh();

        _laser.enabled = showLaser;
        if (showLaser)
        {
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

    // -----------------------------------------------------------------
    //  Helpers
    // -----------------------------------------------------------------

    private Vector2 ForwardDir() => transform.right;

    private static Vector2 AngleToDir(float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    // -----------------------------------------------------------------
    //  Scene Gizmos
    // -----------------------------------------------------------------

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Transform body   = transform;
        Vector3 origin   = body.position;
        float   fwdAngle = body.eulerAngles.z;

        Gizmos.color = new Color(1f, 0.96f, 0.7f, 0.6f);
        DrawGizmoArc(origin, fwdAngle, viewRadius, viewAngle, 24);
        Gizmos.DrawLine(origin, origin + (Vector3)AngleToDir(fwdAngle - viewAngle * 0.5f) * viewRadius);
        Gizmos.DrawLine(origin, origin + (Vector3)AngleToDir(fwdAngle + viewAngle * 0.5f) * viewRadius);

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
