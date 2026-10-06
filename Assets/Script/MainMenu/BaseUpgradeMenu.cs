using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main-menu "Base Upgrade" shop panel (permanent upgrades paid with coins).
/// Separate from the in-game wave upgrades. Does NOT change MainMenu.cs or your existing buttons.
///
/// ─── SETUP (GameMenu scene) ─────────────────────────────────────────────────
///   1. In your existing Canvas create the shop panel (background, title, coins text,
///      3 rows, a Back button).
///   2. Add this script to an object that stays ACTIVE (e.g. the Canvas), and drag the
///      panel into 'panel'. The panel is hidden on start.
///   3. Each row gets BaseUpgradeRowUI (pick its stat). Drag the 3 rows into 'rows'.
///   4. coinsText → TMP text that shows the player's coins.
///   5. closeButton (optional) → wired by code to Close().
///   6. When you add the "Upgrade" button to the main menu, set its OnClick to
///      BaseUpgradeMenu.Open  (drag the object with this script into the OnClick slot).
///   7. Optional: mainMenuButtonsRoot → the group with Start/Options/Quit. It is hidden
///      while the shop is open and shown again on Close (leave empty to keep them visible).
///
/// ─── TESTING ────────────────────────────────────────────────────────────────
///   Right-click this component in the Inspector:
///     "DEBUG: +1000 coins"        → gives coins
///     "DEBUG: Reset all progress" → coins and levels back to 0
///   Costs / bonuses are edited in MetaProgression.cs.
/// </summary>
public class BaseUpgradeMenu : MonoBehaviour
{
    public GameObject         panel;
    public BaseUpgradeRowUI[] rows;
    public TMP_Text           coinsText;
    public string             coinsFormat = "{0}";
    public Button             closeButton;
    public GameObject         mainMenuButtonsRoot;

    private void Awake()
    {
        if (rows != null)
            foreach (BaseUpgradeRowUI row in rows)
                if (row != null) row.Init(this);

        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (panel != null) panel.SetActive(false);
    }

    public void Open()
    {
        if (panel != null) panel.SetActive(true);
        if (mainMenuButtonsRoot != null) mainMenuButtonsRoot.SetActive(false);
        RefreshAll();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        if (mainMenuButtonsRoot != null) mainMenuButtonsRoot.SetActive(true);
    }

    public void RefreshAll()
    {
        if (coinsText != null) coinsText.text = string.Format(coinsFormat, MetaProgression.Coins);
        if (rows == null) return;
        foreach (BaseUpgradeRowUI row in rows)
            if (row != null) row.Refresh();
    }

    [ContextMenu("DEBUG: +1000 coins")]
    private void DebugAddCoins()
    {
        MetaProgression.AddCoins(1000);
        RefreshAll();
    }

    [ContextMenu("DEBUG: Reset all progress")]
    private void DebugReset()
    {
        MetaProgression.ResetAll();
        RefreshAll();
    }
}
