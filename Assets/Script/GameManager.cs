using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public GameObject goMenu;
    public bool pause;
    void Start()
    {
        
    }

    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
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
    }
    void ResumeGame()
    {
    Time.timeScale = 1f;
    }
    public void RestartGame()
    {
    Time.timeScale = 1f;
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void QuitGame()
    {
    Application.Quit();
    }
}