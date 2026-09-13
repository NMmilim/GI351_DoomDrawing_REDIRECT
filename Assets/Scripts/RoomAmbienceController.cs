using UnityEngine;

public class RoomAmbienceController : MonoBehaviour
{
    public AudioSource crowdAudio;
    public AudioSource footstepAudio;

    public float fadeSpeed = 1f;

    private bool roomCleared = false;

    void Update()
    {
        // เช็คว่าศัตรูหมดหรือยัง
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        if (enemies.Length == 0)
        {
            roomCleared = true;
        }

        // ค่อย ๆ ลดเสียง
        if (roomCleared)
        {
            FadeOut(crowdAudio);
            FadeOut(footstepAudio);
        }
    }

    void FadeOut(AudioSource audioSource)
    {
        if (audioSource == null) return;

        if (audioSource.volume > 0)
        {
            audioSource.volume -= fadeSpeed * Time.deltaTime;
        }
        else
        {
            audioSource.Stop();
        }
    }
}