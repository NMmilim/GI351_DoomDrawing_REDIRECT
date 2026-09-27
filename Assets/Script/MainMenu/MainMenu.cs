using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void Play()
    {
        LevelManager.Instance.LoadScene("MainGame", "CrossFade");
    }

    public void Tutorial()
    {
        LevelManager.Instance.LoadScene("tutorial", "CrossFade");
    }

    public void Quit()
    {
        Application.Quit();
    }
}