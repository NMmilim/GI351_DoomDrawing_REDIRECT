using UnityEngine;
using UnityEngine.SceneManagement;

public class EndTrigger : MonoBehaviour
{
    public string endSceneName = "End";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // กันกรณีเคย pause ไว้
            Time.timeScale = 1f;

            SceneManager.LoadScene(endSceneName);
        }
    }
}
