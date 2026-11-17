using UnityEngine;

/// <summary>
/// Abstract base class for enemy upgrades (applied per wave set or loop).
/// Inherits from ScriptableObject to allow reusable asset definitions.
/// </summary>
public abstract class EnemyUpgrade : ScriptableObject
{
    [Header("Enemy Upgrade Settings")]
    public int tierLevel = 1;

    /// <summary>
    /// Polymorphic application: applies stat buffs or new behaviors to the enemy.
    /// </summary>
    public abstract void ApplyToEnemy(EnemyBase enemy);

    public virtual void RemoveFromEnemy(EnemyBase enemy) { }
}

[CreateAssetMenu(fileName = "EnemyUpgrade_Health", menuName = "Upgrades/Enemy/Health")]
public class EnemyHealthUpgrade : EnemyUpgrade
{
    public int bonusHp = 3;

    public override void ApplyToEnemy(EnemyBase enemy)
    {
        if (enemy != null)
        {
            enemy.maxHp += bonusHp;
        }
    }
}

[CreateAssetMenu(fileName = "EnemyUpgrade_Speed", menuName = "Upgrades/Enemy/Speed")]
public class EnemySpeedUpgrade : EnemyUpgrade
{
    public float speedBonus = 1.0f;

    public override void ApplyToEnemy(EnemyBase enemy)
    {
        if (enemy != null)
        {
            enemy.moveSpeed += speedBonus;
        }
    }
}
