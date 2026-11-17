using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

/// <summary>
/// Melee enemy archetype inheriting from EnemyBase.
/// Implements melee pursuit and attack abilities (flickering telegraph cone, windup freeze, active hitbox cone),
/// while inheriting core attributes, health, coin rewards, and damage handling from EnemyBase.
/// </summary>
public class Enemy_Close : EnemyBase
{
    // -----------------------------------------------------------------
    //  Active Enemies Registry (Maintained for backwards compatibility)
    // -----------------------------------------------------------------
    public static readonly List<Enemy_Close> Active = new List<Enemy_Close>();

    // -----------------------------------------------------------------
    //  State Machine
    // -----------------------------------------------------------------
    public enum MeleeState
    {
        Chasing,
        Windup,
        Recovering
    }

    // -----------------------------------------------------------------
    //  Inspector - Archetype Abilities & Combat Settings
    // -----------------------------------------------------------------
    [Header("Melee Combat Attributes")]
    [Tooltip("Damage dealt to the player per melee hit.")]
    public int damage = 25;

    [Tooltip("Degrees per second the body rotates toward the target.")]
    public float rotationSpeed = 360f;

    [Header("Melee Attack & Telegraph")]
    [Tooltip("Distance from the player (in world units) to initiate an attack windup.")]
    public float attackRange = 1.6f;

    [Tooltip("Cooldown in seconds after a swing before the enemy can attack again.")]
    public float attackCooldown = 1.0f;

    [Tooltip("Duration in seconds of the telegraph windup before the swing executes.")]
    public float swingWindupTime = 0.45f;

    [Tooltip("Arc angle in degrees for the melee swing cone.")]
    [Range(20f, 180f)]
    public float swingAngle = 75f;

    [Tooltip("Reach of the melee swing cone in world units.")]
    public float swingRange = 1.6f;

    [Tooltip("Warning color of the short telegraph cone.")]
    public Color telegraphColor = new Color(1f, 0.5f, 0.1f, 0.35f);

    [Tooltip("Active color of the hitbox strike cone.")]
    public Color hitboxColor = new Color(1f, 0.15f, 0.15f, 0.8f);

    [Tooltip("Duration in seconds that the active hitbox cone stays active.")]
    public float hitboxDuration = 0.12f;

    [Tooltip("Optional custom prefab for the hitbox cone. If null, a procedural cone hitbox GameObject is spawned.")]
    public GameObject hitboxPrefab;

    // -----------------------------------------------------------------
    //  Runtime State
    // -----------------------------------------------------------------
    private float      _attackTimer;
    private float      _stateTimer;
    private MeleeState _state = MeleeState.Chasing;

    // Telegraph visual cone components
    private GameObject   _telegraphObj;
    private MeshFilter   _telegraphFilter;
    private MeshRenderer _telegraphRenderer;
    private Mesh         _telegraphMesh;

    public MeleeState CurrentState => _state;

    // -----------------------------------------------------------------
    //  Lifecycle
    // -----------------------------------------------------------------
    protected override void OnEnable()
    {
        base.OnEnable();
        if (!Active.Contains(this)) Active.Add(this);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        Active.Remove(this);
        HideTelegraphCone();
    }

    protected override void Awake()
    {
        base.Awake();
        SetupTelegraphCone();
    }

    protected override void Start()
    {
        base.Start();

        if (_stats != null)
        {
            damage = _stats.Damage;
        }

        if (_ai != null)
        {
            _ai.maxSpeed = moveSpeed;
        }
    }

    private void Update()
    {
        if (_isDead) return;

        if (_player == null)
        {
            GameObject playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null) _player = playerGO.transform;
            if (_player == null) return;
        }

        switch (_state)
        {
            case MeleeState.Chasing:
                TickChasing();
                break;

            case MeleeState.Windup:
                TickWindup();
                break;

            case MeleeState.Recovering:
                TickRecovering();
                break;
        }
    }

    // -----------------------------------------------------------------
    //  State Logic
    // -----------------------------------------------------------------
    private void TickChasing()
    {
        if (_attackTimer > 0f)
            _attackTimer -= Time.deltaTime;

        // Pathfind towards player
        SetDestination(_player.position);
        UpdateRotation();

        // Check if player is within attack range
        float distToPlayer = Vector2.Distance(transform.position, _player.position);
        if (distToPlayer <= attackRange && _attackTimer <= 0f)
        {
            EnterWindup();
        }
    }

    private void EnterWindup()
    {
        _state      = MeleeState.Windup;
        _stateTimer = swingWindupTime;

        StopMoving();

        // Lock physics completely during attack windup so external contacts do not shove the enemy
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.constraints    = RigidbodyConstraints2D.FreezeAll;
        }

        UpdateRotation();
        ShowTelegraphCone();
    }

    private void TickWindup()
    {
        _stateTimer -= Time.deltaTime;

        // Flicker warning cone on and off rapidly during windup (16 Hz)
        if (_telegraphRenderer != null)
        {
            bool isFlickerOn = (Mathf.FloorToInt(_stateTimer * 16f) % 2) == 0;
            _telegraphRenderer.enabled = isFlickerOn;
        }

        // Smoothly adjust orientation towards player during windup
        UpdateRotation();

        if (_stateTimer <= 0f)
        {
            ExecuteSwing();
        }
    }

    private void ExecuteSwing()
    {
        HideTelegraphCone();
        SpawnHitboxCone();

        // Restore normal rotation constraint
        if (_rb != null)
        {
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        _state       = MeleeState.Recovering;
        _stateTimer  = 0.1f; // Short recovery frame before moving again
        _attackTimer = attackCooldown;
    }

    private void TickRecovering()
    {
        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f)
        {
            _state = MeleeState.Chasing;
            if (_ai != null)
                _ai.isStopped = false;
        }
    }

    // -----------------------------------------------------------------
    //  Hitbox & Telegraph Cone Systems
    // -----------------------------------------------------------------
    private void SetupTelegraphCone()
    {
        _telegraphObj = new GameObject("SwingTelegraphCone");
        _telegraphObj.transform.SetParent(transform, false);
        _telegraphObj.transform.localPosition = Vector3.zero;
        _telegraphObj.transform.localRotation = Quaternion.identity;

        _telegraphFilter   = _telegraphObj.AddComponent<MeshFilter>();
        _telegraphRenderer = _telegraphObj.AddComponent<MeshRenderer>();
        _telegraphMesh     = new Mesh { name = "TelegraphConeMesh" };
        _telegraphFilter.mesh = _telegraphMesh;

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                     ?? Shader.Find("Sprites/Default")
                     ?? Shader.Find("Unlit/Color");
        if (shader != null)
        {
            _telegraphRenderer.material = new Material(shader) { color = telegraphColor };
        }
        _telegraphRenderer.sortingOrder = 100;
        _telegraphObj.SetActive(false);
    }

    private void ShowTelegraphCone()
    {
        if (_telegraphObj == null) return;

        // Compensate for parent transform scale so cone size is always in world units
        Vector3 pScale = transform.lossyScale;
        float sx = Mathf.Abs(pScale.x) > 0.0001f ? 1f / pScale.x : 1f;
        float sy = Mathf.Abs(pScale.y) > 0.0001f ? 1f / pScale.y : 1f;
        _telegraphObj.transform.localScale = new Vector3(sx, sy, 1f);

        BuildConeMesh(_telegraphMesh, swingAngle, swingRange, 16);
        _telegraphObj.SetActive(true);
        if (_telegraphRenderer != null) _telegraphRenderer.enabled = true;
    }

    private void HideTelegraphCone()
    {
        if (_telegraphObj != null)
            _telegraphObj.SetActive(false);
    }

    /// <summary>
    /// Spawns the active second cone named "hitbox" that deals damage to the player.
    /// </summary>
    private void SpawnHitboxCone()
    {
        GameObject hitbox;
        if (hitboxPrefab != null)
        {
            hitbox = Instantiate(hitboxPrefab, transform.position, transform.rotation);
            hitbox.name = "hitbox";
        }
        else
        {
            hitbox = new GameObject("hitbox");
            hitbox.transform.position = transform.position;
            hitbox.transform.rotation = transform.rotation;
            MeleeHitbox mb = hitbox.AddComponent<MeleeHitbox>();
            mb.Init(swingRange, swingAngle, damage, hitboxDuration, hitboxColor);
        }
    }

    private void BuildConeMesh(Mesh mesh, float coneAngle, float coneRadius, int resolution)
    {
        if (mesh == null) return;

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

        mesh.Clear();
        mesh.vertices  = verts;
        mesh.uv        = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
    }

    private void UpdateRotation()
    {
        if (_player == null) return;

        Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
        if (dir.sqrMagnitude < 0.001f) return;

        // Sprite and hitbox face UP: subtract 90 degrees so transform.up points toward target
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        float newAngle    = Mathf.MoveTowardsAngle(transform.eulerAngles.z, targetAngle, rotationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
    }

    // -----------------------------------------------------------------
    //  Death Override
    // -----------------------------------------------------------------
    protected override void Die()
    {
        HideTelegraphCone();
        Active.Remove(this);
        base.Die();
    }

    // -----------------------------------------------------------------
    //  Gizmos (Scene View Indicator)
    // -----------------------------------------------------------------
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = (_state == MeleeState.Windup) ? hitboxColor : telegraphColor;
        Vector3 origin = transform.position;
        float halfAngle = swingAngle * 0.5f;
        Vector3 leftDir  = Quaternion.Euler(0, 0, halfAngle) * transform.up;
        Vector3 rightDir = Quaternion.Euler(0, 0, -halfAngle) * transform.up;
        Gizmos.DrawLine(origin, origin + leftDir * swingRange);
        Gizmos.DrawLine(origin, origin + rightDir * swingRange);
        Gizmos.DrawLine(origin + leftDir * swingRange, origin + rightDir * swingRange);
    }
}
