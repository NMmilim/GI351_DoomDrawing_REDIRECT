using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public GameObject goMenu;
    public GameObject _player;
    public bool pause;
    void Start()
    {
       _player = GameObject.FindGameObjectWithTag("Player");
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
    void OnTriggerEnter2D(Collider2D other)
       
    {
        if (other.CompareTag("Player"))
        {
           SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}