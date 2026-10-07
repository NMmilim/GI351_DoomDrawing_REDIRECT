using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple HUD for player health — slider + optional text + optional damage flash.
/// Mirrors style of `AmmoUI`. Polls `PlayerManagement.Instance.hp` each frame so it
/// updates immediately when `Player.RegisterHit` / `RegisterHeal` change the value.
/// </summary>
public class HealthUI : MonoBehaviour
{
    // Optional: assign in Inspector, otherwise found at runtime.
    public PlayerManagement playerManagement;

    // UI: assign a Slider (fill shows %), optional numeric text.
    public Slider hpSlider;
    public TMP_Text hpText;
    
    // Optional damage flash: an Image over the screen that briefly flashes red when damaged.
    public Image damageFlashImage;
    public float flashDuration = 0.25f;
    public Color flashColor = new Color(1f, 0f, 0f, 0.6f);

    int _lastHp = -1;
    float _lastMaxHp = -1f;
    Coroutine _flashCoroutine;

    private void Start()
    {
        if (playerManagement == null) playerManagement = PlayerManagement.Instance;
        // Ensure slider max is correct at start
        RefreshMaxHp();
        // Subscribe to stat changes (maxhp changes).
        if (playerManagement != null) playerManagement.OnStatsChanged += RefreshMaxHp;
        // Initialize visuals
        UpdateVisuals(force: true);
    }

    private void OnDestroy()
    {
        if (playerManagement != null) playerManagement.OnStatsChanged -= RefreshMaxHp;
    }

    private void RefreshMaxHp()
    {
        if (playerManagement == null) return;
        if (hpSlider != null)
        {
            hpSlider.maxValue = Mathf.Max(1, playerManagement.maxhp);
        }
        _lastMaxHp = playerManagement.maxhp;
    }

    private void Update()
    {
        UpdateVisuals();
    }

    private void UpdateVisuals(bool force = false)
    {
        if (playerManagement == null) return;

        int hp = playerManagement.hp;
        int maxhp = playerManagement.maxhp;

        // Detect damage for flash (only when hp decreased).
        if (!force && _lastHp >= 0 && hp < _lastHp)
        {
            TriggerDamageFlash();
        }

        // Update slider
        if (hpSlider != null)
        {
            // Keep slider max in sync if something changed without OnStatsChanged firing.
            if (hpSlider.maxValue != maxhp) hpSlider.maxValue = Mathf.Max(1, maxhp);
            hpSlider.value = Mathf.Clamp(hp, 0, maxhp);
        }

        // Update text
        if (hpText != null)
        {
            hpText.text = $"{hp} / {maxhp}";
        }

        _lastHp = hp;
    }

    private void TriggerDamageFlash()
    {
        if (damageFlashImage == null) return;
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(DamageFlashCoroutine());
    }

    private System.Collections.IEnumerator DamageFlashCoroutine()
    {
        float elapsed = 0f;
        damageFlashImage.gameObject.SetActive(true);
        // initialize color
        damageFlashImage.color = flashColor;

        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flashDuration;
            // fade alpha from initial to 0
            var c = damageFlashImage.color;
            c.a = Mathf.Lerp(flashColor.a, 0f, t);
            damageFlashImage.color = c;
            yield return null;
        }

        damageFlashImage.gameObject.SetActive(false);
        _flashCoroutine = null;
    }
}
