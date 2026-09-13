using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    public Health playerHealth;
    public Slider healthSlider;
    public Image fillImage;

    void Start()
    {
        healthSlider.maxValue = playerHealth.maxHealth;
    }

    void Update()
    {
        float hpPercent = (float)playerHealth.CurrentHealth / playerHealth.maxHealth;

        fillImage.color = Color.Lerp(Color.red, Color.green, hpPercent);

        healthSlider.value = playerHealth.CurrentHealth;
    }
}