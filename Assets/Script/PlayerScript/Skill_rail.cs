using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Railgun skill (key 2) — now with a CHARGE-UP before it fires.
///
/// ─── FLOW ───────────────────────────────────────────────────────────────────
///   Press 2 (cooldown ready) → charging starts:
///       • muzzle light glows up (chargeGlowColor / chargeGlowIntensity)
///       • optional aim laser grows thicker (telegraph)
///       • optional chargeEffect GameObject is enabled (particles etc.)
///       • optional charge sound plays
///       • player is slowed (moveSpeedWhileCharging) and can't use the pistol
///   After chargeTime seconds → the rail bullet fires in the CURRENT aim direction
///   (you can keep aiming while charging), then cooldown (cooldown x 3) starts.
///
/// ─── INSPECTOR ──────────────────────────────────────────────────────────────
///   Old fields (bulletPrefab, firepos, muzzleLight, ...) are unchanged.
///   holdToCharge = true  → player must HOLD 2; releasing early cancels the shot.
///   holdToCharge = false → tap 2 once, it fires by itself when charged (default).
///   aimLineMask          → set to your Wall layer so the laser stops at walls.
///                          Leave as "Nothing" to draw a fixed-length laser.
///   chargeAudioSource    → OPTIONAL AudioSource with a charge clip. Used instead of
///                          SoundManager so the sound can be STOPPED on cancel.
///
/// ─── SOUND IDs (SoundLibrary) ───────────────────────────────────────────────
///   "Railgun_Charge" (only used if chargeAudioSource is empty), "Railgun_Fire" (in RailBullet)
/// </summary>
public class Skill_rail : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firepos;
    [SerializeField] private Light2D muzzleLight;
    public GameObject RailBullet;
    private Coroutine _flashCoroutine;
    [Header("MuzzleLight settings")]
    [SerializeField] float peakIntensity = 8f;
    [SerializeField] float flashDuration = 0.08f;

    [Header("Charge")]
    public KeyCode fireKey = KeyCode.Alpha2;
    [Tooltip("Seconds of charging before the shot is released.")]
    public float chargeTime = 0.8f;
    [Tooltip("If true the key must be held the whole time; releasing early cancels.")]
    public bool holdToCharge = false;
    [Tooltip("Move speed multiplier while charging (1 = no slow, 0 = rooted).")]
    [Range(0f, 1f)] public float moveSpeedWhileCharging = 0.5f;

    [Header("Charge visuals / audio")]
    public Color chargeGlowColor = new Color(0.35f, 0.85f, 1f);
    public float chargeGlowIntensity = 4f;
    [Tooltip("Optional object (particles, sprite) enabled only while charging.")]
    public GameObject chargeEffect;
    [Tooltip("Optional. If set, plays/stops this instead of SoundManager 'Railgun_Charge'.")]
    public AudioSource chargeAudioSource;

    [Header("Aim laser (telegraph)")]
    public bool showAimLine = true;
    [Tooltip("Optional. Auto-created if empty.")]
    public LineRenderer aimLine;
    public LayerMask aimLineMask;
    public float aimLineLength   = 30f;
    public float aimLineMaxWidth = 0.12f;
    public Color aimLineColor    = new Color(0.35f, 0.85f, 1f, 0.8f);

    /// <summary>True while ANY railgun is charging (Shooting uses this to block the pistol).</summary>
    public static bool IsAnyCharging { get; private set; }

    public bool  IsCharging     { get; private set; }
    public float ChargeProgress { get; private set; }

    /// <summary>Seconds until the railgun can be used again.</summary>
    public float CooldownRemaining =>
        PlayerManagement.Instance == null ? 0f : Mathf.Max(0f, PlayerManagement.Instance.nextFireTime - Time.time);

    public float CooldownDuration =>
        PlayerManagement.Instance == null ? 0f : PlayerManagement.Instance.cooldown * 3f;

    private float _chargeTimer;
    private Color _lightBaseColor = Color.white;

    private void Start()
    {
        if (muzzleLight != null) _lightBaseColor = muzzleLight.color;
        if (chargeEffect != null) chargeEffect.SetActive(false);
        if (showAimLine && aimLine == null) aimLine = CreateAimLine();
        if (aimLine != null) aimLine.enabled = false;
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        PlayerManagement pm = PlayerManagement.Instance;
        if (pm == null) return;

        if (pm.IsDead)
        {
            if (IsCharging) CancelCharge();
            return;
        }

        if (!IsCharging)
        {
            if (Input.GetKeyDown(fireKey) && Time.time >= pm.nextFireTime)
                BeginCharge();
            return;
        }

        // ---- Charging -------------------------------------------------
        if (holdToCharge && !Input.GetKey(fireKey))
        {
            CancelCharge();
            return;
        }

        _chargeTimer  += Time.deltaTime;
        ChargeProgress = chargeTime <= 0f ? 1f : Mathf.Clamp01(_chargeTimer / chargeTime);
        UpdateChargeVisuals();

        if (ChargeProgress >= 1f)
        {
            EndChargeVisuals();
            RailGun();
            pm.nextFireTime = Time.time + (pm.cooldown * 3);
        }
    }

    private void OnDisable()
    {
        if (IsCharging) CancelCharge();
    }

    // -----------------------------------------------------------------
    //  Charge
    // -----------------------------------------------------------------

    private void BeginCharge()
    {
        IsCharging     = true;
        IsAnyCharging  = true;
        _chargeTimer   = 0f;
        ChargeProgress = 0f;

        if (PlayerManagement.Instance != null)
            PlayerManagement.Instance.speedMultiplier = moveSpeedWhileCharging;

        if (_flashCoroutine != null) { StopCoroutine(_flashCoroutine); _flashCoroutine = null; }
        if (chargeEffect != null) chargeEffect.SetActive(true);
        if (aimLine != null) aimLine.enabled = true;

        if (chargeAudioSource != null) chargeAudioSource.Play();
        else SoundManager.Instance?.PlaySound3D("Railgun_Charge", firepos.position);
    }

    /// <summary>Stops charging without firing (no cooldown is consumed).</summary>
    public void CancelCharge()
    {
        EndChargeVisuals();
        if (muzzleLight != null) muzzleLight.intensity = 0f;
    }

    private void EndChargeVisuals()
    {
        IsCharging     = false;
        IsAnyCharging  = false;
        ChargeProgress = 0f;

        if (PlayerManagement.Instance != null && !PlayerManagement.Instance.IsDead)
            PlayerManagement.Instance.speedMultiplier = 1f;

        if (chargeEffect != null) chargeEffect.SetActive(false);
        if (aimLine != null) aimLine.enabled = false;
        if (chargeAudioSource != null) chargeAudioSource.Stop();
        if (muzzleLight != null) muzzleLight.color = _lightBaseColor;
    }

    private void UpdateChargeVisuals()
    {
        // Ease-in so the glow "builds up" and spikes near the end
        float t = ChargeProgress * ChargeProgress;

        if (muzzleLight != null)
        {
            muzzleLight.color = Color.Lerp(_lightBaseColor, chargeGlowColor, ChargeProgress);
            // small flicker for energy feel
            float flicker = 1f + Mathf.Sin(Time.time * 60f) * 0.08f * ChargeProgress;
            muzzleLight.intensity = chargeGlowIntensity * t * flicker;
        }

        if (aimLine != null && firepos != null)
        {
            Vector3 start = firepos.position;
            Vector3 dir   = firepos.up;
            float   len   = aimLineLength;

            if (aimLineMask.value != 0)
            {
                RaycastHit2D hit = Physics2D.Raycast(start, dir, aimLineLength, aimLineMask);
                if (hit.collider != null) len = hit.distance;
            }

            aimLine.SetPosition(0, start);
            aimLine.SetPosition(1, start + dir * len);

            float width = Mathf.Lerp(0.01f, aimLineMaxWidth, t);
            aimLine.startWidth = width;
            aimLine.endWidth   = width * 0.5f;

            Color c = aimLineColor;
            c.a *= Mathf.Lerp(0.2f, 1f, ChargeProgress);
            aimLine.startColor = c;
            aimLine.endColor   = new Color(c.r, c.g, c.b, 0f);
        }
    }

    private LineRenderer CreateAimLine()
    {
        GameObject go = new GameObject("RailAimLine");
        go.transform.SetParent(transform, false);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.positionCount     = 2;
        lr.useWorldSpace     = true;
        lr.numCapVertices    = 2;
        lr.sortingOrder      = 50;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null) lr.material = new Material(shader);
        return lr;
    }

    // -----------------------------------------------------------------
    //  Fire (original logic)
    // -----------------------------------------------------------------

        void RailGun()
        {
            RailBullet = Instantiate(bulletPrefab, firepos.position, firepos.rotation);
            if (muzzleLight == null) return;

            if (_flashCoroutine != null)

                StopCoroutine(_flashCoroutine);

            _flashCoroutine = StartCoroutine(MuzzleFlashLight());
        }
        IEnumerator MuzzleFlashLight()
        {
            muzzleLight.intensity = peakIntensity;
            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                muzzleLight.intensity = Mathf.Lerp(peakIntensity, 0f, elapsed / flashDuration);
                yield return null;
            }
            muzzleLight.intensity = 0f;
            _flashCoroutine = null;
        }
}
