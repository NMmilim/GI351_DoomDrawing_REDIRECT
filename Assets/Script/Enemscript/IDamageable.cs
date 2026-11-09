using UnityEngine;

/// <summary>
/// Common contract for any actor capable of receiving damage and dying.
/// Decouples weapon bullets (Bullet, RailBullet) from concrete enemy classes.
/// </summary>
public interface IDamageable
{
    bool IsDead { get; }
    void RegisterHit(int amount);
}

/// <summary>
/// Contract for allies that can receive and react to squad alert broadcasts.
/// </summary>
public interface IAlertable
{
    void ReceiveAlert(Vector3 knownPlayerPos);
}

