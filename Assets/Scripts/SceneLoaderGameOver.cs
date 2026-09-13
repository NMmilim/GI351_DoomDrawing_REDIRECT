using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoaderGameOver : MonoBehaviour
{
    public GameObject gameOverUI;

    void Start()
    {
        gameOverUI.SetActive(false);
    }

    public void ShowGameOver()
    {
        gameOverUI.SetActive(true);
        Time.timeScale = 0f; // หยุดเกม
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; // กลับเวลาปกติ
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("StartMenu");
    }
}