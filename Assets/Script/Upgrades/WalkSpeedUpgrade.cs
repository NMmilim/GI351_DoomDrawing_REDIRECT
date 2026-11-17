using UnityEngine;

/// <summary>
/// Walk speed upgrade: increases movement speed across the arena.
/// Inherits from PlayerUpgrade to provide polymorphic execution.
/// </summary>
[CreateAssetMenu(fileName = "Upgrade_WalkSpeed", menuName = "Upgrades/Player/WalkSpeed")]
public class WalkSpeedUpgrade : PlayerUpgrade
{
    public override void ApplyToPlayer(PlayerManagement pm)
    {
        if (pm == null) return;
        pm.walkSpeedLevel++;
    }

    public override string GetPreviewText(PlayerManagement pm)
    {
        if (pm == null) return "";
        return $"Speed: {pm.speed:0.0} → {pm.speed + pm.walkSpeedPerLevel:0.0}";
    }
}
