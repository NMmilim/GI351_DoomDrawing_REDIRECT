using UnityEngine;

/// <summary>
/// Homing ricochet upgrade: bullets seek nearby enemies upon wall impact instead of bouncing physically.
/// Inherits from PlayerUpgrade to provide polymorphic execution.
/// </summary>
[CreateAssetMenu(fileName = "Upgrade_HomingRicochet", menuName = "Upgrades/Player/HomingRicochet")]
public class HomingRicochetUpgrade : PlayerUpgrade
{
    public override void ApplyToPlayer(PlayerManagement pm)
    {
        if (pm == null) return;
        pm.homingRicochet = true;
    }

    public override string GetPreviewText(PlayerManagement pm)
    {
        return "Ricochets seek enemies";
    }
}
