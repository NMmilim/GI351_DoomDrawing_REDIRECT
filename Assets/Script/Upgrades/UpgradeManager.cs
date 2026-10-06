using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum UpgradeType
{
    Ricochet,
    FireRate,
    WalkSpeed,
    HomingRicochet,
    MagazineCapacity
}

public enum UpgradeRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}

[Serializable]
public class UpgradeDefinition
{
    public UpgradeType   type;
    public string        title;
    [TextArea(2, 4)]
    public string        description;
    public UpgradeRarity rarity;
    [Tooltip("Higher = shows up more often. Homing should be very low.")]
    [Min(0f)] public float weight = 10f;
    [Tooltip("How many times this can be picked in one run. 0 = unlimited.")]
    [Min(0)] public int maxStacks = 0;

    public UpgradeDefinition(UpgradeType type, string title, string description,
                             UpgradeRarity rarity, float weight, int maxStacks)
    {
        this.type        = type;
        this.title       = title;
        this.description = description;
        this.rarity      = rarity;
        this.weight      = weight;
        this.maxStacks   = maxStacks;
    }
}

/// <summary>
/// IN-RUN upgrades: after every cleared wave the game pauses and shows 3 cards.
/// The player picks ONE. Upgrades reset when the run ends (scene reload).
///
/// ─── SETUP (easy way) ───────────────────────────────────────────────────────
///   Open the gameplay scene → menu  Tools > DoomDrawing > Create Gameplay UI.
///   It builds the panel + 3 cards and wires everything below automatically.
///
/// ─── SETUP (manual) ─────────────────────────────────────────────────────────
///   1. Put this on any GameObject in the gameplay scene.
///   2. panel   = root object of the upgrade screen (disabled by default).
///   3. cards   = 3 objects with UpgradeCardUI (each has a Button).
///   4. stageText (optional) = TMP text for "Stage 2".
///   EnemySpawner calls ShowChoices() automatically when a wave is cleared.
///
/// ─── BALANCING ──────────────────────────────────────────────────────────────
///   Edit the "pool" list in the Inspector: title, description, rarity,
///   weight (chance) and maxStacks. The actual stat strength per level lives in
///   PlayerManagement ("In-run upgrade strength").
///   Right-click the component → "Reset Pool To Defaults" to restore defaults.
///
/// ─── CONTROLS ───────────────────────────────────────────────────────────────
///   Click a card, or press 1 / 2 / 3.
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject     panel;
    public UpgradeCardUI[] cards;
    public TMP_Text       stageText;

    [Header("Upgrade Pool")]
    public List<UpgradeDefinition> pool = DefaultPool();

    [Header("Rarity Colours")]
    public Color commonColor    = new Color(0.80f, 0.80f, 0.80f);
    public Color rareColor      = new Color(0.30f, 0.65f, 1.00f);
    public Color epicColor      = new Color(0.70f, 0.35f, 1.00f);
    public Color legendaryColor = new Color(1.00f, 0.75f, 0.15f);

    public bool IsOpen { get; private set; }

    /// <summary>Fired after the player picks an upgrade.</summary>
    public event Action<UpgradeDefinition> OnUpgradePicked;

    private readonly Dictionary<UpgradeType, int> _stacks = new Dictionary<UpgradeType, int>();
    private readonly List<UpgradeDefinition>      _offered = new List<UpgradeDefinition>();

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) Pick(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) Pick(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) Pick(2);
    }

    // -----------------------------------------------------------------
    //  Public API
    // -----------------------------------------------------------------

    /// <summary>
    /// Rolls upgrades and opens the screen (pauses the game).
    /// Returns false if nothing could be shown (no UI / everything maxed).
    /// </summary>
    public bool ShowChoices(int clearedWaveNumber)
    {
        if (IsOpen) return true;
        if (panel == null || cards == null || cards.Length == 0)
        {
            Debug.LogWarning("[UpgradeManager] UI not assigned — skipping upgrade screen. " +
                             "Run Tools > DoomDrawing > Create Gameplay UI.");
            return false;
        }
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return false;

        RollChoices(cards.Length);
        if (_offered.Count == 0) return false;

        if (stageText != null) stageText.text = $"Stage {clearedWaveNumber + 1}";

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null) continue;
            if (i < _offered.Count)
            {
                UpgradeDefinition def = _offered[i];
                cards[i].gameObject.SetActive(true);
                cards[i].Bind(def, GetPreviewText(def.type), GetRarityColor(def.rarity), i, Pick);
            }
            else
            {
                cards[i].gameObject.SetActive(false);
            }
        }

        panel.SetActive(true);
        IsOpen = true;
        Time.timeScale = 0f;
        return true;
    }

    public void Pick(int index)
    {
        if (!IsOpen || index < 0 || index >= _offered.Count) return;

        UpgradeDefinition def = _offered[index];
        Apply(def.type);

        _stacks.TryGetValue(def.type, out int n);
        _stacks[def.type] = n + 1;

        SoundManager.Instance?.PlaySound2D("Upgrade_Pick");

        panel.SetActive(false);
        IsOpen = false;
        Time.timeScale = 1f;

        OnUpgradePicked?.Invoke(def);
    }

    public int GetStacks(UpgradeType type) => _stacks.TryGetValue(type, out int n) ? n : 0;

    public Color GetRarityColor(UpgradeRarity r)
    {
        switch (r)
        {
            case UpgradeRarity.Rare:      return rareColor;
            case UpgradeRarity.Epic:      return epicColor;
            case UpgradeRarity.Legendary: return legendaryColor;
            default:                      return commonColor;
        }
    }

    // -----------------------------------------------------------------
    //  Rolling (weighted, no duplicates per roll)
    // -----------------------------------------------------------------

    private void RollChoices(int count)
    {
        _offered.Clear();

        List<UpgradeDefinition> candidates = new List<UpgradeDefinition>();
        foreach (UpgradeDefinition def in pool)
        {
            if (def == null || def.weight <= 0f) continue;
            if (def.maxStacks > 0 && GetStacks(def.type) >= def.maxStacks) continue;
            candidates.Add(def);
        }

        while (_offered.Count < count && candidates.Count > 0)
        {
            float total = 0f;
            foreach (UpgradeDefinition c in candidates) total += c.weight;

            float roll = UnityEngine.Random.value * total;
            int chosen = candidates.Count - 1;
            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= candidates[i].weight;
                if (roll <= 0f) { chosen = i; break; }
            }

            _offered.Add(candidates[chosen]);
            candidates.RemoveAt(chosen);
        }
    }

    // -----------------------------------------------------------------
    //  Applying
    // -----------------------------------------------------------------

    private void Apply(UpgradeType type)
    {
        PlayerManagement pm = PlayerManagement.Instance;
        if (pm == null) return;

        switch (type)
        {
            case UpgradeType.Ricochet:         pm.UpgRicochet++;        break;
            case UpgradeType.FireRate:         pm.fireRateLevel++;      break;
            case UpgradeType.WalkSpeed:        pm.walkSpeedLevel++;     break;
            case UpgradeType.MagazineCapacity: pm.magazineLevel++;      break;
            case UpgradeType.HomingRicochet:   pm.homingRicochet = true; break;
        }

        pm.RecalculateStats();
    }

    /// <summary>"current → next" line shown on the card so players see exactly what changes.</summary>
    private string GetPreviewText(UpgradeType type)
    {
        PlayerManagement pm = PlayerManagement.Instance;
        if (pm == null) return "";

        switch (type)
        {
            case UpgradeType.Ricochet:
            {
                int now = 1 + pm.UpgRicochet;   // base ricochet = 1
                return $"Bounces: {now} → {now + 1}";
            }
            case UpgradeType.FireRate:
            {
                float now  = 1f / pm.fireInterval;
                float next = 1f / Mathf.Max(pm.minFireInterval, pm.fireInterval * pm.fireRateMultiplierPerLevel);
                return $"Shots/sec: {now:0.0} → {next:0.0}";
            }
            case UpgradeType.WalkSpeed:
                return $"Speed: {pm.speed:0.0} → {pm.speed + pm.walkSpeedPerLevel:0.0}";
            case UpgradeType.MagazineCapacity:
                return $"Magazine: {pm.magazineSize} → {pm.magazineSize + pm.magazinePerLevel}";
            case UpgradeType.HomingRicochet:
                return "Ricochets seek enemies";
            default:
                return "";
        }
    }

    // -----------------------------------------------------------------
    //  Defaults
    // -----------------------------------------------------------------

    private static List<UpgradeDefinition> DefaultPool()
    {
        return new List<UpgradeDefinition>
        {
            new UpgradeDefinition(UpgradeType.Ricochet, "Ricochet+",
                "Your bullets bounce off walls one more time before breaking.",
                UpgradeRarity.Rare, 20f, 0),

            new UpgradeDefinition(UpgradeType.FireRate, "Quick Trigger",
                "Shoot faster. Lowers the delay between pistol shots.",
                UpgradeRarity.Common, 30f, 8),

            new UpgradeDefinition(UpgradeType.WalkSpeed, "Light Feet",
                "Move faster around the arena.",
                UpgradeRarity.Common, 30f, 8),

            new UpgradeDefinition(UpgradeType.MagazineCapacity, "Extended Mag",
                "Fire more bullets before you have to reload.",
                UpgradeRarity.Common, 30f, 0),

            new UpgradeDefinition(UpgradeType.HomingRicochet, "Seeker Rounds",
                "After hitting an enemy, your bullet ricochets straight into the next enemy " +
                "instead of bouncing off at an angle. Uses your ricochet count.",
                UpgradeRarity.Legendary, 3f, 1),
        };
    }

    [ContextMenu("Reset Pool To Defaults")]
    private void ResetPoolToDefaults()
    {
        pool = DefaultPool();
    }
}
