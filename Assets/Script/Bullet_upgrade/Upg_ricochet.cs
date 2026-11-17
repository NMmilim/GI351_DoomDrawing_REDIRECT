using UnityEngine;

/// <summary>
/// Ricochet upgrade: increases the number of wall bounces for pistol bullets.
/// Inherits from PlayerUpgrade to provide polymorphic execution.
/// </summary>
[CreateAssetMenu(fileName = "Upgrade_Ricochet", menuName = "Upgrades/Player/Ricochet")]
public class Upg_ricochet : PlayerUpgrade
{
    [Tooltip("Extra bounces granted per level of this upgrade.")]
    public int extraBounces = 1;

    public override void ApplyToPlayer(PlayerManagement pm)
    {
        if (pm == null) return;
        pm.UpgRicochet += extraBounces;
    }

    public override string GetPreviewText(PlayerManagement pm)
    {
        if (pm == null) return "";
        int now = 1 + pm.UpgRicochet;   // Base ricochet = 1
        return $"Bounces: {now} → {now + extraBounces}";
    }
}
