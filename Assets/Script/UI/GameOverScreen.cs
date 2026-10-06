using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "YOU DIED" screen. Only connects your team's UI to the game — no visuals are created.
///
/// ─── SETUP ──────────────────────────────────────────────────────────────────
///   1. Build the death screen in the gameplay Canvas (title, texts, 3 buttons).
///   2. Add this script to an object that stays ACTIVE (e.g. the Canvas or GameManager),
///      and drag the screen's root into 'root'. It's hidden on Awake.
///   3. Drag the buttons into restartButton / menuButton / quitButton.
///      Their OnClick is wired by code — no need to set it in the Inspector.
///   4. Drag this object into GameManager → Game Over Screen.
///
///   Optional texts:
///     statsText : "Wave reached: 3   Kills: 17"
///     coinsText : "+120 coins   (Total: 560)"
///   canvasGroup (optional, on root) → fades the screen in.
/// </summary>
public class GameOverScreen : MonoBehaviour
{
    public GameObject root;
    public CanvasGroup canvasGroup;
    public float fadeInTime = 0.4f;

    [Header("Texts (optional)")]
    public TMP_Text statsText;
    public TMP_Text coinsText;

    [Header("Buttons")]
    public Button restartButton;
    public Button menuButton;
    public Button quitButton;

    private float _fadeTimer = -1f;

    private void Awake()
    {
        if (root != null) root.SetActive(false);

        if (restartButton != null) restartButton.onClick.AddListener(() => GameManager.Instance?.RestartGame());
        if (menuButton    != null) menuButton.onClick.AddListener(() => GameManager.Instance?.ReturnToMenu());
        if (quitButton    != null) quitButton.onClick.AddListener(() => GameManager.Instance?.QuitGame());
    }

    /// <summary>Called by GameManager after the player dies.</summary>
    public void Show(int wavesCleared, int kills, int coinsEarned, int totalCoins)
    {
        if (statsText != null) statsText.text = $"Wave reached: {wavesCleared + 1}    Kills: {kills}";
        if (coinsText != null) coinsText.text = $"+{coinsEarned} coins    (Total: {totalCoins})";

        if (root != null) root.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            _fadeTimer = 0f;
        }
    }

    private void Update()
    {
        if (_fadeTimer < 0f || canvasGroup == null) return;

        _fadeTimer += Time.unscaledDeltaTime;   // game is paused, so use unscaled time
        canvasGroup.alpha = fadeInTime <= 0f ? 1f : Mathf.Clamp01(_fadeTimer / fadeInTime);
        if (canvasGroup.alpha >= 1f) _fadeTimer = -1f;
    }
}
