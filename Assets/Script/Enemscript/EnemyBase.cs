using System;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

/// <summary>
/// Abstract base class for all enemy archetypes in the game.
/// Enforces the separation of Attributes/Properties (HP, Coin Rewards, Movement, Damage handling, Blood VFX)
/// from archetype-specific Abilities and Behaviors (implemented by derived classes such as Enemy_Range and Enemy_Close).
/// </summary>
[RequireComponent(typeof(AIPath))]
[RequireComponent(typeof(Seeker))]
[RequireComponent(typeof(Rigidbody2D))]
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    // -----------------------------------------------------------------
    //  Global Active Enemies Registry
    // -----------------------------------------------------------------
    public static readonly List<EnemyBase> ActiveEnemies = new List<EnemyBase>();

    // -----------------------------------------------------------------
    //  Attributes / Properties (คุณสมบัติ)
    // -----------------------------------------------------------------
    [Header("Base Attributes & Stats")]
    [Tooltip("Health points before dying.")]
    public int maxHp = 3;

    [Tooltip("Coins added to the run total when this enemy dies (banked on Game Over).")]
    public int coinReward = 5;

    [Tooltip("Movement speed while pursuing or maneuvering.")]
    public float moveSpeed = 3f;

    [Header("Visual Effects (Damage / Blood)")]
    [Tooltip("Blood splat prefab instantiated upon taking damage.")]
    public GameObject bloodSplatPrefab;

    [Range(1, 8)]
    public int splatsPerHit = 3;
    public float splatRadius = 0.6f;

    // -----------------------------------------------------------------
    //  Runtime State & References
    // -----------------------------------------------------------------
    protected int _hp;
    protected bool _isDead;
    protected AIPath _ai;
    protected Seeker _seeker;
    protected Rigidbody2D _rb;
    protected Transform _player;
    protected EnemyStats _stats;

    public int CurrentHp => _hp;
    public bool IsDead => _isDead;
    public Transform PlayerTarget => _player;

    public event Action<int> OnDamaged;
    public event Action OnDied;

    // -----------------------------------------------------------------
    //  Lifecycle
    // -----------------------------------------------------------------
    protected virtual void Awake()
    {
        _ai     = GetComponent<AIPath>();
        _seeker = GetComponent<Seeker>();
        _rb     = GetComponent<Rigidbody2D>();
        _stats  = GetComponent<EnemyStats>();

        if (_ai != null)
        {
            _ai.enableRotation = false; // Rotation is controlled by the enemy archetype / abilities
            _ai.gravity        = Vector3.zero;
            _ai.orientation    = OrientationMode.YAxisForward;
        }

        // Freeze rotation on Rigidbody2D by default so physics collisions do not spin the enemy
        if (_rb != null)
        {
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    protected virtual void OnEnable()
    {
        if (!ActiveEnemies.Contains(this))
            ActiveEnemies.Add(this);
    }

    protected virtual void OnDisable()
    {
        ActiveEnemies.Remove(this);
    }

    protected virtual void Start()
    {
        if (_stats != null)
        {
            maxHp      = _stats.MaxHp;
            coinReward = _stats.CoinReward;
            moveSpeed  = _stats.MoveSpeed;
        }

        _hp = maxHp;

        GameObject playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
        {
            _player = playerGO.transform;
        }
        else
        {
            Debug.LogWarning($"[{GetType().Name}] '{name}': No 'Player' tagged object found.");
        }
    }

    // -----------------------------------------------------------------
    //  Damage & Death (IDamageable Implementation)
    // -----------------------------------------------------------------
    public virtual void RegisterHit(int amount)
    {
        if (_isDead || amount <= 0) return;

        SpawnBlood(transform.position);
        _hp -= amount;
        OnDamaged?.Invoke(amount);

        if (_hp <= 0)
        {
            Die();
        }
    }

    protected virtual void SpawnBlood(Vector2 position)
    {
        if (bloodSplatPrefab == null) return;

        for (int i = 0; i < splatsPerHit; i++)
        {
            Vector2 offset   = UnityEngine.Random.insideUnitCircle * splatRadius;
            Vector3 splatPos = (Vector3)(position + offset);
            Instantiate(bloodSplatPrefab, splatPos, Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f)));
        }
    }

    protected virtual void Die()
    {
        if (_isDead) return;
        _isDead = true;

        if (_ai != null)
            _ai.isStopped = true;

        ActiveEnemies.Remove(this);
        OnDied?.Invoke();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEnemyKilled(coinReward);
        }

        Destroy(gameObject);
    }

    // -----------------------------------------------------------------
    //  Navigation Helpers (A* Pathfinding Wrappers)
    // -----------------------------------------------------------------
    public virtual void SetDestination(Vector3 pos)
    {
        if (_ai == null) return;
        _ai.isStopped   = false;
        _ai.destination = pos;
    }

    public virtual void SetSpeed(float speed)
    {
        if (_ai != null) _ai.maxSpeed = speed;
    }

    public virtual void StopMoving()
    {
        if (_ai != null) _ai.isStopped = true;
    }

    public virtual bool HasArrived(float threshold = 0.35f)
    {
        return _ai != null && !_ai.pathPending && _ai.remainingDistance <= threshold;
    }
}
