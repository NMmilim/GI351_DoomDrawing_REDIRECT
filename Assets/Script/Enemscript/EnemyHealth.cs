using System;
using UnityEngine;

/// <summary>
/// Handles health state, damage registration, blood splatter effects, and death notifications.
/// Implements IDamageable for polymorphic hit detection from all weapon types.
/// </summary>
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Visual Effects")]
    [Tooltip("Blood splat prefab instantiated upon taking damage.")]
    public GameObject bloodSplatPrefab;

    [Range(1, 8)]
    public int splatsPerHit = 3;
    public float splatRadius = 0.6f;

    private EnemyStats _stats;
    private int _currentHp;
    private bool _isDead;

    public int  CurrentHp => _currentHp;
    public bool IsDead    => _isDead;

    public event Action<int> OnDamaged;
    public event Action      OnDied;

    private void Awake()
    {
        _stats = GetComponent<EnemyStats>();
    }

    private void Start()
    {
        int initialHp = (_stats != null) ? _stats.MaxHp : 3;
        _currentHp = Mathf.Max(1, initialHp);
    }

    public void HealToFull()
    {
        if (_isDead) return;
        _currentHp = (_stats != null) ? _stats.MaxHp : _currentHp;
    }

    public void RegisterHit(int amount)
    {
        if (_isDead || amount <= 0) return;

        SpawnBlood();
        _currentHp -= amount;
        OnDamaged?.Invoke(amount);

        if (_currentHp <= 0)
        {
            Die();
        }
    }

    private void SpawnBlood()
    {
        if (bloodSplatPrefab == null) return;

        for (int i = 0; i < splatsPerHit; i++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * splatRadius;
            Vector3 splatPos = transform.position + new Vector3(offset.x, offset.y, 0f);
            Instantiate(bloodSplatPrefab, splatPos, Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f)));
        }
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        OnDied?.Invoke();

        int reward = (_stats != null) ? _stats.CoinReward : 5;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEnemyKilled(reward);
        }

        Destroy(gameObject);
    }
}

