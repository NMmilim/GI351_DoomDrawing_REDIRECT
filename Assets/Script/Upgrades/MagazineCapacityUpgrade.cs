using UnityEngine;

/// <summary>
/// Magazine capacity upgrade: increases bullet capacity per clip.
/// Inherits from PlayerUpgrade to provide polymorphic execution.
/// </summary>
[CreateAssetMenu(fileName = "Upgrade_MagazineCapacity", menuName = "Upgrades/Player/MagazineCapacity")]
public class MagazineCapacityUpgrade : PlayerUpgrade
{
    public override void ApplyToPlayer(PlayerManagement pm)
    {
        if (pm == null) return;
        pm.magazineLevel++;
    }

    public override string GetPreviewText(PlayerManagement pm)
    {
        if (pm == null) return "";
        return $"Magazine: {pm.magazineSize} → {pm.magazineSize + pm.magazinePerLevel}";
    }
}
