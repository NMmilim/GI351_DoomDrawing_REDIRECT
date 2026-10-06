using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Railgun cooldown icon: railgun picture + a dark shadow on top that slides DOWN
/// as the cooldown runs out. The shadow speed always matches the real cooldown.
///
/// ─── RECOMMENDED SETUP (Filled Image, no Slider needed) ─────────────────────
///   RailgunIcon            (Image  - the railgun picture)
///     └─ CooldownShadow    (Image  - same sprite or a plain square, colour black ~70% alpha)
///                           Image Type  = Filled
///                           Fill Method = Vertical
///                           Fill Origin = Bottom
///   Put this script on RailgunIcon (or anywhere) and drag CooldownShadow into 'shadowImage'.
///   fillAmount 1 = fully dark (just fired) → 0 = clear (ready). Because the origin is
///   Bottom, the TOP edge of the shadow moves down — the "shadow slowly goes down" look.
///   Tip: use the railgun sprite itself for the shadow so it only darkens the gun shape.
///
/// ─── IF YOU ALREADY BUILT IT WITH A SLIDER ──────────────────────────────────
///   Drag it into 'shadowSlider' instead (Min 0, Max 1, Direction Bottom To Top,
///   Interactable off, delete the Handle). Value 1 = dark → 0 = ready.
///
/// ─── OPTIONAL EXTRAS (leave empty if not needed) ────────────────────────────
///   chargeFillImage : Filled Image that fills 0→1 while the railgun is CHARGING.
///   cooldownText    : TMP text showing seconds left ("2.4"). Hidden when ready.
///   readyPulse      : when the cooldown finishes the icon does a quick scale "pop".
///   iconImage       : the railgun picture; tinted with cooldownTint while on cooldown.
///
///   'rail' is found automatically if left empty.
/// </summary>
public class RailgunCooldownUI : MonoBehaviour
{
    [Header("Source (auto-found if empty)")]
    public Skill_rail rail;

    [Header("Shadow (use ONE of these)")]
    public Image  shadowImage;
    public Slider shadowSlider;

    [Header("Optional")]
    public Image    iconImage;
    public Color    readyTint    = Color.white;
    public Color    cooldownTint = new Color(0.6f, 0.6f, 0.6f);
    public Image    chargeFillImage;
    public TMP_Text cooldownText;

    [Header("Ready pop")]
    public bool  readyPulse      = true;
    public float pulseScale      = 1.2f;
    public float pulseDuration   = 0.2f;

    private bool  _wasOnCooldown;
    private float _pulseTimer;
    private Vector3 _baseScale;

    private void Start()
    {
        if (rail == null) rail = FindAnyObjectByType<Skill_rail>();
        _baseScale = transform.localScale;

        if (shadowSlider != null)
        {
            shadowSlider.minValue     = 0f;
            shadowSlider.maxValue     = 1f;
            shadowSlider.interactable = false;
        }
    }

    private void Update()
    {
        float cooldown = rail != null ? rail.CooldownFraction : 0f;   // 1 → 0
        float charge   = rail != null ? rail.ChargeProgress   : 0f;   // 0 → 1
        bool  onCooldown = cooldown > 0f;

        if (shadowImage  != null) shadowImage.fillAmount = cooldown;
        if (shadowSlider != null) shadowSlider.value     = cooldown;

        if (iconImage != null) iconImage.color = onCooldown ? cooldownTint : readyTint;

        if (chargeFillImage != null)
        {
            chargeFillImage.fillAmount = charge;
            chargeFillImage.enabled    = rail != null && rail.IsCharging;
        }

        if (cooldownText != null)
        {
            cooldownText.enabled = onCooldown;
            if (onCooldown) cooldownText.text = rail.CooldownRemaining.ToString("0.0");
        }

        // "Pop" the icon the moment it becomes ready
        if (_wasOnCooldown && !onCooldown && readyPulse) _pulseTimer = pulseDuration;
        _wasOnCooldown = onCooldown;

        if (_pulseTimer > 0f)
        {
            _pulseTimer -= Time.unscaledDeltaTime;
            float t = 1f - Mathf.Clamp01(_pulseTimer / pulseDuration);   // 0 → 1
            float s = Mathf.Lerp(pulseScale, 1f, t);
            transform.localScale = _baseScale * s;
        }
        else
        {
            transform.localScale = _baseScale;
        }
    }
}
