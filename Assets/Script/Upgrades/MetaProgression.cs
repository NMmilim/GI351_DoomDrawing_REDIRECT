using UnityEngine;

/// <summary>
/// PERMANENT (between runs) progression: coins + base upgrades bought in the main menu.
///
/// ─── WHAT THIS IS ───────────────────────────────────────────────────────────
///   A static helper (NOT a MonoBehaviour) — do NOT attach it to anything.
///   Everything is saved with PlayerPrefs, so it survives quitting the game.
///
/// ─── HOW THE LOOP WORKS ─────────────────────────────────────────────────────
///   1. Player dies  → GameManager.TriggerGameOver() calls MetaProgression.AddCoins(earned).
///   2. Main menu    → BaseUpgradeMenu spends coins with MetaProgression.TryPurchase(stat).
///   3. Next run     → PlayerManagement.RecalculateStats() reads BonusMaxHp / BonusSpeed / BonusMagazine.
///
/// ─── TUNING (edit the values below) ─────────────────────────────────────────
///   • Costs[]          : price of level 1..5. Keep it expensive so players grind.
///   • HpPerLevel etc.  : how strong each level is.
///   • MaxLevel         : cap (design says 5 — if you change it, add entries to Costs[]).
///
/// ─── DEBUG ──────────────────────────────────────────────────────────────────
///   BaseUpgradeMenu has right-click (context menu) entries:
///   "DEBUG: +1000 coins" and "DEBUG: Reset all progress".
/// </summary>
public static class MetaProgression
{
    public enum Stat
    {
        MaxHp,
        MaxSpeed,
        MaxMagazine
    }

    public const int MaxLevel = 5;

    // Price to BUY level 1, 2, 3, 4, 5 (index 0 = first purchase).
    private static readonly int[] Costs = { 150, 350, 700, 1200, 2000 };

    // Bonus granted PER level
    public const int   HpPerLevel       = 20;    // +20 max HP per level   (lv5 = +100)
    public const float SpeedPerLevel    = 0.4f;  // +0.4 move speed        (lv5 = +2.0)
    public const int   MagazinePerLevel = 2;     // +2 bullets per mag     (lv5 = +10)

    private const string CoinsKey       = "Meta_Coins";
    private const string LevelKeyPrefix = "Meta_Level_";

    // -----------------------------------------------------------------
    //  Coins
    // -----------------------------------------------------------------

    public static int Coins => PlayerPrefs.GetInt(CoinsKey, 0);

    public static void AddCoins(int amount)
    {
        if (amount <= 0) return;
        PlayerPrefs.SetInt(CoinsKey, Coins + amount);
        PlayerPrefs.Save();
    }

    // -----------------------------------------------------------------
    //  Levels
    // -----------------------------------------------------------------

    public static int GetLevel(Stat stat) => PlayerPrefs.GetInt(LevelKeyPrefix + stat, 0);

    public static bool IsMaxed(Stat stat) => GetLevel(stat) >= MaxLevel;

    /// <summary>Cost of the NEXT level, or -1 if already maxed.</summary>
    public static int GetNextCost(Stat stat)
    {
        int lvl = GetLevel(stat);
        if (lvl >= MaxLevel) return -1;
        return Costs[Mathf.Clamp(lvl, 0, Costs.Length - 1)];
    }

    public static bool CanAfford(Stat stat)
    {
        int cost = GetNextCost(stat);
        return cost >= 0 && Coins >= cost;
    }

    /// <summary>Spend coins and raise the level by 1. Returns false if maxed / too poor.</summary>
    public static bool TryPurchase(Stat stat)
    {
        if (!CanAfford(stat)) return false;

        PlayerPrefs.SetInt(CoinsKey, Coins - GetNextCost(stat));
        PlayerPrefs.SetInt(LevelKeyPrefix + stat, GetLevel(stat) + 1);
        PlayerPrefs.Save();
        return true;
    }

    // -----------------------------------------------------------------
    //  Bonuses (read by PlayerManagement at the start of every run)
    // -----------------------------------------------------------------

    public static int   BonusMaxHp    => GetLevel(Stat.MaxHp)       * HpPerLevel;
    public static float BonusSpeed    => GetLevel(Stat.MaxSpeed)    * SpeedPerLevel;
    public static int   BonusMagazine => GetLevel(Stat.MaxMagazine) * MagazinePerLevel;

    // -----------------------------------------------------------------
    //  Text helpers for UI
    // -----------------------------------------------------------------

    public static string GetDisplayName(Stat stat)
    {
        switch (stat)
        {
            case Stat.MaxHp:       return "Max HP";
            case Stat.MaxSpeed:    return "Max Speed";
            case Stat.MaxMagazine: return "Bullets per Mag";
            default:               return stat.ToString();
        }
    }

    /// <summary>e.g. "+40 HP" for the current total bonus.</summary>
    public static string GetBonusText(Stat stat)
    {
        switch (stat)
        {
            case Stat.MaxHp:       return $"+{BonusMaxHp} HP";
            case Stat.MaxSpeed:    return $"+{BonusSpeed:0.0} speed";
            case Stat.MaxMagazine: return $"+{BonusMagazine} bullets";
            default:               return "";
        }
    }

    // -----------------------------------------------------------------
    //  Debug
    // -----------------------------------------------------------------

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(CoinsKey);
        foreach (Stat s in System.Enum.GetValues(typeof(Stat)))
            PlayerPrefs.DeleteKey(LevelKeyPrefix + s);
        PlayerPrefs.Save();
    }
}
