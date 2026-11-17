using UnityEngine;

/// <summary>
/// Fire rate upgrade: accelerates pistol attack frequency by decreasing shot interval.
/// Inherits from PlayerUpgrade to provide polymorphic execution.
/// </summary>
[CreateAssetMenu(fileName = "Upgrade_FireRate", menuName = "Upgrades/Player/FireRate")]
public class FireRateUpgrade : PlayerUpgrade
{
    public override void ApplyToPlayer(PlayerManagement pm)
    {
        if (pm == null) return;
        pm.fireRateLevel++;
    }

    public override string GetPreviewText(PlayerManagement pm)
    {
        if (pm == null) return "";
        float now  = 1f / pm.fireInterval;
        float next = 1f / Mathf.Max(pm.minFireInterval, pm.fireInterval * pm.fireRateMultiplierPerLevel);
        return $"Shots/sec: {now:0.0} → {next:0.0}";
    }
}
