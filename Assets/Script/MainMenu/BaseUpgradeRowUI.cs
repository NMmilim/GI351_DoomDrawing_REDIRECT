using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ONE row in the main-menu Base Upgrade shop (Max HP / Max Speed / Bullets per Mag).
///
/// ─── SETUP ──────────────────────────────────────────────────────────────────
///   Build the row in your Canvas, add this script, choose 'stat', drag references.
///   All texts / pips are optional; 'buyButton' is needed to buy.
///     nameText  : "Max HP"
///     levelText : "Lv 2 / 5"
///     bonusText : "+40 HP"
///     costText  : "350"  (shows "MAX" when maxed)
///     levelPips : 5 Images (boxes/stars). Filled ones use pipOnColor.
///   The buyButton's OnClick is wired by code. It greys out when too poor or maxed.
/// </summary>
public class BaseUpgradeRowUI : MonoBehaviour
{
    public MetaProgression.Stat stat;

    public TMP_Text nameText;
    public TMP_Text levelText;
    public TMP_Text bonusText;
    public TMP_Text costText;
    public Button   buyButton;
    public Image[]  levelPips;
    public Color    pipOnColor  = new Color(1f, 0.8f, 0.2f);
    public Color    pipOffColor = new Color(1f, 1f, 1f, 0.15f);

    private BaseUpgradeMenu _menu;

    public void Init(BaseUpgradeMenu menu)
    {
        _menu = menu;
        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(Buy);
            buyButton.onClick.AddListener(Buy);
        }
    }

    private void Buy()
    {
        if (MetaProgression.TryPurchase(stat))
        {
            SoundManager.Instance?.PlaySound2D("Upgrade_Buy");
            if (_menu != null) _menu.RefreshAll();
            else Refresh();
        }
    }

    public void Refresh()
    {
        int  level = MetaProgression.GetLevel(stat);
        int  cost  = MetaProgression.GetNextCost(stat);
        bool maxed = MetaProgression.IsMaxed(stat);

        if (nameText  != null) nameText.text  = MetaProgression.GetDisplayName(stat);
        if (levelText != null) levelText.text = $"Lv {level} / {MetaProgression.MaxLevel}";
        if (bonusText != null) bonusText.text = MetaProgression.GetBonusText(stat);
        if (costText  != null) costText.text  = maxed ? "MAX" : cost.ToString();
        if (buyButton != null) buyButton.interactable = MetaProgression.CanAfford(stat);

        if (levelPips != null)
            for (int i = 0; i < levelPips.Length; i++)
                if (levelPips[i] != null) levelPips[i].color = i < level ? pipOnColor : pipOffColor;
    }
}
