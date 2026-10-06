using System;
using UnityEngine;

/// <summary>
/// Central player stats. Every other script READS from here.
///
/// ─── HOW STATS ARE CALCULATED ───────────────────────────────────────────────
///   FINAL value = Base value  +  permanent upgrade (MetaProgression, bought in menu)
///                             +  in-run upgrade   (UpgradeManager, picked after each wave)
///
///   • Edit the "Base Stats" fields in the Inspector to balance the game.
///   • maxhp / speed / magazineSize / fireInterval are OVERWRITTEN at runtime by
///     RecalculateStats() — changing them in the Inspector does nothing.
///   • After changing any upgrade counter from code, call RecalculateStats().
///
/// ─── SETUP ──────────────────────────────────────────────────────────────────
///   One instance per gameplay scene (already in TestPlace). Not DontDestroyOnLoad,
///   so in-run upgrades automatically reset when the scene reloads.
/// </summary>
public class PlayerManagement : MonoBehaviour
{
    public static PlayerManagement Instance;

    [Header("Player Status")]
    public int hp;
    [Tooltip("FINAL max HP (calculated at runtime). Edit Base Max Hp instead.")]
    public int maxhp;

    public float armor;
    [Tooltip("FINAL move speed (calculated at runtime). Edit Base Speed instead.")]
    public float speed;

    [Header("Base Stats (before any upgrade)")]
    public int   baseMaxHp        = 100;
    public float baseSpeed        = 5f;
    [Tooltip("Pistol bullets per magazine before upgrades.")]
    public int   baseMagazineSize = 8;
    [Tooltip("Seconds between pistol shots before upgrades (lower = faster).")]
    public float baseFireInterval = 0.25f;

    [Header("Player Combat stats")]
    public int dmg;
    [Tooltip("Railgun cooldown = cooldown x 3 seconds (starts after the shot leaves the barrel).")]
    public float cooldown=1;
   public float nextFireTime=0;
    [Tooltip("FINAL bullets per magazine (calculated at runtime).")]
    public int   magazineSize;
    [Tooltip("FINAL seconds between pistol shots (calculated at runtime).")]
    public float fireInterval;

    [Header("Player Upgrades (in-run, reset every run)")]
    [Tooltip("Extra ricochets from wave upgrades. Each point = +1 wall bounce.")]
    public int UpgRicochet;
    public float Piercing;
    public float BulletSpeed;
    public int  fireRateLevel;
    public int  walkSpeedLevel;
    public int  magazineLevel;
    [Tooltip("Rare upgrade: bullets ricochet into the next enemy instead of bouncing physically.")]
    public bool homingRicochet;

    [Header("In-run upgrade strength (per level)")]
    [Tooltip("Each Fire Rate level multiplies the shot interval by this (0.85 = 15% faster).")]
    [Range(0.5f, 0.99f)]
    public float fireRateMultiplierPerLevel = 0.85f;
    [Tooltip("Fire interval can never go below this.")]
    public float minFireInterval   = 0.06f;
    public float walkSpeedPerLevel = 0.5f;
    public int   magazinePerLevel  = 2;

    /// <summary>Temporary multiplier (e.g. Railgun slows you while charging). Not saved.</summary>
    [NonSerialized] public float speedMultiplier = 1f;

    public bool IsDead { get; private set; }

    /// <summary>Fired whenever RecalculateStats() runs (HUD / Shooting listen to this).</summary>
    public event Action OnStatsChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        RecalculateStats();
        hp = maxhp;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Rebuilds every FINAL stat from base + permanent + in-run upgrades.</summary>
    public void RecalculateStats()
    {
        int oldMax = maxhp;

        maxhp        = baseMaxHp + MetaProgression.BonusMaxHp;
        speed        = baseSpeed + MetaProgression.BonusSpeed + walkSpeedLevel * walkSpeedPerLevel;
        magazineSize = Mathf.Max(1, baseMagazineSize + MetaProgression.BonusMagazine + magazineLevel * magazinePerLevel);
        fireInterval = Mathf.Max(minFireInterval, baseFireInterval * Mathf.Pow(fireRateMultiplierPerLevel, fireRateLevel));

        // If max HP went up mid-run, give the player the extra HP too.
        if (oldMax > 0 && maxhp > oldMax) hp += maxhp - oldMax;
        hp = Mathf.Clamp(hp, 0, maxhp);

        OnStatsChanged?.Invoke();
    }

    /// <summary>Called by Player when HP reaches 0.</summary>
    public void MarkDead()
    {
        IsDead = true;
        speedMultiplier = 0f;
    }
}
