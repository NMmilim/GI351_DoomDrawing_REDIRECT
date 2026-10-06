using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ONE upgrade card on the wave-end "Select Upgrade" screen.
/// This script only FILLS IN your team's card UI — it does not create any visuals.
///
/// ─── SETUP ──────────────────────────────────────────────────────────────────
///   1. Build the card in your Canvas however you like (frame, art, texts...).
///   2. Add this component to the card root.
///   3. Drag in the references below. EVERY field is optional except 'button'.
///        button          → the Button the player clicks (usually on the card root)
///        titleText       → e.g. "Ricochet+"
///        descriptionText → the explanation text
///        previewText     → live "current → next" line, e.g. "Bounces: 1 → 2"
///        rarityText      → e.g. "LEGENDARY"
///        hotkeyText      → shows "1", "2", "3"
///        rarityTintTargets → Images tinted with the rarity colour (frame, badge...)
///   4. Drag the 3 cards into UpgradeManager → Cards.
///   You do NOT need to set the Button's OnClick in the Inspector — it's wired by code.
/// </summary>
public class UpgradeCardUI : MonoBehaviour
{
    public Button   button;
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public TMP_Text previewText;
    public TMP_Text rarityText;
    public TMP_Text hotkeyText;
    [Tooltip("Images coloured with the rarity colour (card frame, badge, glow...).")]
    public Image[]  rarityTintTargets;

    public UpgradeDefinition Current { get; private set; }

    private int         _index;
    private Action<int> _onPicked;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(HandleClick);
    }

    /// <summary>Called by UpgradeManager every time the screen opens.</summary>
    public void Bind(UpgradeDefinition def, string preview, Color rarityColor, int index, Action<int> onPicked)
    {
        Current   = def;
        _index    = index;
        _onPicked = onPicked;

        if (titleText       != null) titleText.text       = def.title;
        if (descriptionText != null) descriptionText.text = def.description;
        if (previewText     != null) previewText.text     = preview;
        if (hotkeyText      != null) hotkeyText.text      = (index + 1).ToString();
        if (rarityText      != null)
        {
            rarityText.text  = def.rarity.ToString().ToUpper();
            rarityText.color = rarityColor;
        }

        if (rarityTintTargets != null)
            foreach (Image img in rarityTintTargets)
                if (img != null) img.color = rarityColor;
    }

    private void HandleClick()
    {
        _onPicked?.Invoke(_index);
    }
}
