using UnityEngine;

/// <summary>
/// Active melee hitbox cone spawned by Enemy_Close during an attack swing.
/// Faces UP (transform.up) and visually renders the active strike cone in game.
/// Damages any Player caught inside its arc and automatically self-destructs after duration.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MeleeHitbox : MonoBehaviour
{
    [Header("Hitbox Properties")]
    public float range = 1.6f;
    public float angle = 75f;
    public int   damage = 25;
    public float duration = 0.12f;
    public Color hitColor = new Color(1f, 0.15f, 0.15f, 0.85f);

    private MeshFilter   _filter;
    private MeshRenderer _renderer;
    private Mesh         _mesh;
    private Material     _mat;

    private void Awake()
    {
        _filter   = GetComponent<MeshFilter>();
        _renderer = GetComponent<MeshRenderer>();
        _mesh     = new Mesh { name = "MeleeHitboxConeMesh" };
        _filter.mesh = _mesh;

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                     ?? Shader.Find("Sprites/Default")
                     ?? Shader.Find("Unlit/Color");
        if (shader != null)
        {
            _mat = new Material(shader) { color = hitColor };
            _renderer.material = _mat;
        }
        _renderer.sortingOrder = 100;
    }

    private void Start()
    {
        BuildConeMesh(angle, range, 16);
        ExecuteHitCheck();
        Destroy(gameObject, duration);
    }

    public void Init(float swingRange, float swingAngle, int swingDamage, float swingDuration, Color color)
    {
        range    = swingRange;
        angle    = swingAngle;
        damage   = swingDamage;
        duration = swingDuration;
        hitColor = color;
        if (_mat != null) _mat.color = hitColor;
        BuildConeMesh(angle, range, 16);
    }

    private void ExecuteHitCheck()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);
        bool hasHit = false;

        foreach (Collider2D col in hits)
        {
            if (col == null) continue;
            Player player = col.GetComponent<Player>();
            if (player != null)
            {
                Vector2 toPlayer = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
                // Hitbox faces UP: check angle relative to transform.up
                if (toPlayer.sqrMagnitude < 0.001f || Vector2.Angle(transform.up, toPlayer) <= angle * 0.5f)
                {
                    player.RegisterHit(damage);
                    hasHit = true;
                }
            }
        }

        if (hasHit && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound3D("BulletImpactFlesh", transform.position);
        }
    }

    private void BuildConeMesh(float coneAngle, float coneRadius, int resolution)
    {
        if (_mesh == null) return;

        int vertCount = resolution + 2;
        Vector3[] verts = new Vector3[vertCount];
        Vector2[] uvs   = new Vector2[vertCount];
        int[]     tris  = new int[resolution * 3];

        verts[0] = Vector3.zero;
        uvs[0]   = new Vector2(0.5f, 0.5f);

        float halfAngle = coneAngle * 0.5f;
        float step      = coneAngle / resolution;

        for (int i = 0; i <= resolution; i++)
        {
            // Centered around 90 degrees (+Y / Up)
            float deg = 90f - halfAngle + step * i;
            float rad = deg * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
            verts[i + 1] = dir * coneRadius;
            uvs[i + 1]   = new Vector2(dir.x * 0.5f + 0.5f, dir.y * 0.5f + 0.5f);
        }

        for (int i = 0; i < resolution; i++)
        {
            int b = i * 3;
            tris[b]     = 0;
            tris[b + 1] = i + 1;
            tris[b + 2] = i + 2;
        }

        _mesh.Clear();
        _mesh.vertices  = verts;
        _mesh.uv        = uvs;
        _mesh.triangles = tris;
        _mesh.RecalculateNormals();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = hitColor;
        Vector3 origin = transform.position;
        float halfAngle = angle * 0.5f;
        Vector3 leftDir  = Quaternion.Euler(0, 0, halfAngle) * transform.up;
        Vector3 rightDir = Quaternion.Euler(0, 0, -halfAngle) * transform.up;
        Gizmos.DrawLine(origin, origin + leftDir * range);
        Gizmos.DrawLine(origin, origin + rightDir * range);
        Gizmos.DrawLine(origin + leftDir * range, origin + rightDir * range);
    }
}
