using UnityEngine;

/// <summary>
/// Damage upgrade: increases base bullet damage dealt to enemies.
/// Inherits from PlayerUpgrade to provide polymorphic execution.
/// </summary>
[CreateAssetMenu(fileName = "Upgrade_Damage", menuName = "Upgrades/Player/Damage")]
public class Upg_dmg : PlayerUpgrade
{
    [Tooltip("Extra damage points added to bullet hits.")]
    public int bonusDamage = 1;

    public override void ApplyToPlayer(PlayerManagement pm)
    {
        if (pm == null) return;
        pm.dmg += bonusDamage;
    }

    public override string GetPreviewText(PlayerManagement pm)
    {
        if (pm == null) return "";
        int now = pm.dmg;
        return $"Damage: {now} → {now + bonusDamage}";
    }
}
