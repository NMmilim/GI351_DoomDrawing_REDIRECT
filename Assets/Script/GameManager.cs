using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gameplay-scene manager: pause, game over, restart / menu / quit, and run coins.
///
/// ─── SETUP ──────────────────────────────────────────────────────────────────
///   • One per gameplay scene (already in TestPlace).
///   • goMenu          : OPTIONAL pause panel. Shown on Esc, hidden on resume.
///   • gameOverScreen  : drag the GameOverScreen object here
///                       (Tools > DoomDrawing > Create Gameplay UI does it for you).
///   • mainMenuSceneName must match the menu scene in Build Settings ("GameMenu").
///
/// ─── COINS ──────────────────────────────────────────────────────────────────
///   Kills (Enemy_Range.coinReward) and cleared waves (coinsPerWaveCleared x wave#)
///   add to RunCoins. On death, RunCoins are banked into MetaProgression and can
///   be spent in the main menu Base Upgrade shop.
///
/// ─── BUTTON HOOKS (public, usable from any UI Button OnClick) ───────────────
///   ResumeGame(), RestartGame(), ReturnToMenu(), QuitGame()
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameObject goMenu;
    public GameObject _player;
    public bool pause;

    [Header("Game Over")]
    public GameOverScreen gameOverScreen;
    public string mainMenuSceneName = "GameMenu";
    [Tooltip("Real-time seconds between death and the Game Over screen appearing.")]
    public float gameOverDelay = 0.8f;

    [Header("Coins (banked into the base-upgrade shop on death)")]
    [Tooltip("Coins for clearing a wave = this x wave number.")]
    public int coinsPerWaveCleared = 20;

    public int  RunCoins     { get; private set; }
    public int  Kills        { get; private set; }
    public int  WavesCleared { get; private set; }
    public bool IsGameOver   { get; private set; }

    public event Action<int> OnRunCoinsChanged;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;   // safety: scene may have been loaded from a paused state
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
       _player = GameObject.FindGameObjectWithTag("Player");
        if (goMenu != null) goMenu.SetActive(false);
    }

    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Don't allow pausing over the Game Over / Upgrade screens
            if (IsGameOver) return;
            if (UpgradeManager.Instance != null && UpgradeManager.Instance.IsOpen) return;

            pause = !pause;
            
            GamePause();
           
        }

    
    }
    void GamePause()
    {
        if (pause) PauseGame();
        else ResumeGame();
    }

    void PauseGame()
    {
    Time.timeScale = 0f;
        if (goMenu != null) goMenu.SetActive(true);
    }
    public void ResumeGame()
    {
    pause = false;
    Time.timeScale = 1f;
        if (goMenu != null) goMenu.SetActive(false);
    }
    public void RestartGame()
    {
    Time.timeScale = 1f;
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void QuitGame()
    {
    Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    /// <summary>Back to the main menu (uses the CrossFade LevelManager if it exists).</summary>
    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        if (LevelManager.Instance != null)
            LevelManager.Instance.LoadScene(mainMenuSceneName, "CrossFade");
        else
            SceneManager.LoadScene(mainMenuSceneName);
    }

    // -----------------------------------------------------------------
    //  Coins / stats
    // -----------------------------------------------------------------

    public void OnEnemyKilled(int coinReward)
    {
        if (IsGameOver) return;
        Kills++;
        AddRunCoins(coinReward);
    }

    /// <summary>Called by EnemySpawner when a wave is cleared (waveNumber starts at 1).</summary>
    public void OnWaveCleared(int waveNumber)
    {
        if (IsGameOver) return;
        WavesCleared = Mathf.Max(WavesCleared, waveNumber);
        AddRunCoins(coinsPerWaveCleared * waveNumber);
    }

    public void AddRunCoins(int amount)
    {
        if (amount <= 0) return;
        RunCoins += amount;
        OnRunCoinsChanged?.Invoke(RunCoins);
    }

    // -----------------------------------------------------------------
    //  Game Over
    // -----------------------------------------------------------------

    /// <summary>Called by Player when HP reaches 0.</summary>
    public void TriggerGameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        pause      = false;
        if (goMenu != null) goMenu.SetActive(false);

        // Bank coins NOW so they're saved even if the player alt-F4s on the death screen.
        MetaProgression.AddCoins(RunCoins);

        StartCoroutine(GameOverRoutine());
    }

    private IEnumerator GameOverRoutine()
    {
        // Short slow-mo beat before freezing
        Time.timeScale = 0.25f;
        yield return new WaitForSecondsRealtime(gameOverDelay);
        Time.timeScale = 0f;

        if (gameOverScreen != null)
            gameOverScreen.Show(WavesCleared, Kills, RunCoins, MetaProgression.Coins);
        else
            Debug.LogWarning("[GameManager] Player died but no GameOverScreen is assigned. " +
                             "Run Tools > DoomDrawing > Create Gameplay UI.");
    }
}