using UnityEngine;

/// <summary>
/// Abstract base class for all player in-run upgrades.
/// Replaces the hardcoded UpgradeType enum + switch statement with polymorphic class inheritance.
/// Each derived upgrade class defines its own application behavior and card preview display logic.
/// </summary>
public abstract class PlayerUpgrade : ScriptableObject
{
    [Header("Upgrade Display Info")]
    public string title;

    [TextArea(2, 4)]
    public string description;

    public UpgradeRarity rarity;

    [Header("Drop Settings")]
    [Tooltip("Higher weight = shows up more frequently in card rolls.")]
    [Min(0f)] public float weight = 10f;

    [Tooltip("How many times this upgrade can be picked in one run. 0 = unlimited.")]
    [Min(0)] public int maxStacks = 0;

    /// <summary>
    /// Polymorphic application: applies this upgrade's specific effect to the player.
    /// </summary>
    public abstract void ApplyToPlayer(PlayerManagement pm);

    /// <summary>
    /// Generates dynamic preview text ("current → next") shown on the upgrade card UI.
    /// </summary>
    public abstract string GetPreviewText(PlayerManagement pm);
}
