using System;
using UnityEngine;

/// <summary>
/// Runtime statistics component attached to an Enemy prefab.
/// Separates enemy statistics from abilities so that upgrades, buffs, or debuffs
/// can modify stats without coupling to AI state machine or ability code.
/// </summary>
public class EnemyStats : MonoBehaviour
{
    [Header("Base Data Preset")]
    [Tooltip("Optional ScriptableObject holding base values. If null, Inspector defaults are used.")]
    public EnemyStatsSO baseStats;

    [Header("Runtime Attributes (Modified by Upgrades)")]
    [SerializeField] private int   maxHp = 3;
    [SerializeField] private int   coinReward = 5;
    [SerializeField] private float moveSpeed = 3.0f;
    [SerializeField] private float roamSpeed = 1.2f;
    [SerializeField] private int   damage = 30;
    [SerializeField] private float fireRate = 0.6f;
    [SerializeField] private int   burstCount = 3;
    [SerializeField] private float viewRadius = 8.0f;

    public int   MaxHp      { get => maxHp; set => maxHp = Mathf.Max(1, value); }
    public int   CoinReward { get => coinReward; set => coinReward = Mathf.Max(0, value); }
    public float MoveSpeed  { get => moveSpeed; set => moveSpeed = Mathf.Max(0f, value); }
    public float RoamSpeed  { get => roamSpeed; set => roamSpeed = Mathf.Max(0f, value); }
    public int   Damage     { get => damage; set => damage = Mathf.Max(0, value); }
    public float FireRate   { get => fireRate; set => fireRate = Mathf.Max(0.05f, value); }
    public int   BurstCount { get => burstCount; set => burstCount = Mathf.Max(1, value); }
    public float ViewRadius { get => viewRadius; set => viewRadius = Mathf.Max(0.5f, value); }

    /// <summary>Fired whenever stats are initialized or modified by upgrades.</summary>
    public event Action OnStatsRecalculated;

    private void Awake()
    {
        InitializeFromBase();
    }

    public void InitializeFromBase()
    {
        if (baseStats != null)
        {
            maxHp      = baseStats.baseMaxHp;
            coinReward = baseStats.baseCoinReward;
            moveSpeed  = baseStats.baseMoveSpeed;
            roamSpeed  = baseStats.baseRoamSpeed;
            damage     = baseStats.baseDamage;
            fireRate   = baseStats.baseFireRate;
            burstCount = baseStats.baseBurstCount;
            viewRadius = baseStats.baseViewRadius;
        }

        OnStatsRecalculated?.Invoke();
    }

    public void NotifyStatsUpdated()
    {
        OnStatsRecalculated?.Invoke();
    }
}

