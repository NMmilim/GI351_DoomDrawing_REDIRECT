using UnityEngine;

/// <summary>
/// Flyweight data configuration for enemy base statistics.
/// Separated from enemy behavior/abilities to allow designer balancing via assets.
/// </summary>
[CreateAssetMenu(fileName = "NewEnemyStats", menuName = "DoomDrawing/Enemy/EnemyStatsSO")]
public class EnemyStatsSO : ScriptableObject
{
    [Header("Survival & Economy")]
    [Tooltip("Base hit points before in-game wave upgrades.")]
    public int baseMaxHp = 3;

    [Tooltip("Coins awarded to the player when this enemy is eliminated.")]
    public int baseCoinReward = 5;

    [Header("Locomotion")]
    [Tooltip("Movement speed when running to cover or pursuing.")]
    public float baseMoveSpeed = 3.0f;

    [Tooltip("Cautious walk speed used while roaming.")]
    public float baseRoamSpeed = 1.2f;

    [Header("Combat & Senses")]
    [Tooltip("Damage dealt per projectile hit or melee contact.")]
    public int baseDamage = 30;

    [Tooltip("Delay in seconds between individual shots.")]
    public float baseFireRate = 0.6f;

    [Tooltip("Number of shots fired in each burst.")]
    [Range(1, 10)]
    public int baseBurstCount = 3;

    [Tooltip("Vision and line-of-sight detection radius in world units.")]
    public float baseViewRadius = 8.0f;
}

