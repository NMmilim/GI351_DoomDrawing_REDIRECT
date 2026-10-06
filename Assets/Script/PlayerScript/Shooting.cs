using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Player main gun (pistol).
///
/// ─── RULES ──────────────────────────────────────────────────────────────────
///   • Limited bullets per magazine  (size = PlayerManagement.magazineSize).
///   • Infinite reloads — no reserve ammo.
///   • Reload: press R any time, or it auto-reloads when the mag runs dry.
///   • Can't shoot while reloading or while the Railgun is charging.
///   • Fire rate = PlayerManagement.fireInterval (upgradeable).
///
/// ─── INSPECTOR ──────────────────────────────────────────────────────────────
///   bulletPrefab / firepos / muzzleLight : same as before (muzzleLight is optional now).
///   reloadTime        : seconds a reload takes.
///   holdToFire        : true = hold LMB for auto fire at the fire rate. false = click per shot.
///   gunAnimator       : OPTIONAL Animator for shoot / reload animations. Parameters it uses
///                       (all optional, missing ones are skipped, names editable in Inspector):
///                         Trigger "Fire"        - every pistol shot
///                         Trigger "Reload"      - reload starts
///                         Bool    "IsReloading" - true during the whole reload
///                         Float   "ReloadSpeed" - reloadClipLength / reloadTime. Tick the Reload
///                                                 state's Speed > Multiplier > Parameter and pick
///                                                 this, so the animation always lasts reloadTime.
///
/// ─── FOR UI ─────────────────────────────────────────────────────────────────
///   Read CurrentAmmo, MagazineSize, IsReloading, ReloadProgress (0..1),
///   or subscribe to OnAmmoChanged. AmmoUI.cs already does this.
///
/// ─── SOUND IDs (add them to SoundLibrary; missing ones are silently skipped) ─
///   "Pistol_Fire" (existing), "Pistol_Reload", "Pistol_Empty"
/// </summary>
public class Shooting : MonoBehaviour
{
    public GameObject bulletPrefab;
    [SerializeField] private Light2D muzzleLight;
    [Header("MuzzleLight settings")]
    [SerializeField] private float peakIntensity = 8f;
    [SerializeField] private float flashDuration = 0.08f;

           public Transform firepos;

    [Header("Magazine / Reload")]
    [Tooltip("Seconds needed to reload a full magazine.")]
    public float reloadTime = 1.2f;
    public KeyCode reloadKey = KeyCode.R;
    [Tooltip("Automatically start reloading when the magazine becomes empty.")]
    public bool autoReloadWhenEmpty = true;

    [Header("Fire mode")]
    [Tooltip("Hold left mouse to keep firing at the current fire rate.")]
    public bool holdToFire = true;

    [Header("Optional animation (see summary)")]
    public Animator gunAnimator;
    public string fireTrigger       = "Fire";
    public string reloadTrigger     = "Reload";
    public string reloadingBool     = "IsReloading";
    public string reloadSpeedFloat  = "ReloadSpeed";
    [Tooltip("Length (seconds) of the reload animation clip. Used to compute ReloadSpeed.")]
    public float  reloadClipLength  = 1f;

    public static Shooting Instance { get; private set; }

    public int   CurrentAmmo    { get; private set; }
    public bool  IsReloading    => _reloadCoroutine != null;
    public float ReloadProgress { get; private set; }
    public int   MagazineSize   => PlayerManagement.Instance != null ? PlayerManagement.Instance.magazineSize : 8;

    /// <summary>Fired when ammo count / reload state changes.</summary>
    public event Action OnAmmoChanged;

    private Coroutine _flashCoroutine;
    private Coroutine _reloadCoroutine;
    private float     _nextShotTime;
    private int       _lastKnownMagSize;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _lastKnownMagSize = MagazineSize;
        CurrentAmmo       = _lastKnownMagSize;

        if (PlayerManagement.Instance != null)
            PlayerManagement.Instance.OnStatsChanged += HandleStatsChanged;

        OnAmmoChanged?.Invoke();
    }

    private void OnDestroy()
    {
        if (PlayerManagement.Instance != null)
            PlayerManagement.Instance.OnStatsChanged -= HandleStatsChanged;
        if (Instance == this) Instance = null;
    }

    private void OnDisable()
    {
        // Coroutines die when disabled — make sure we don't get stuck in "reloading".
        _reloadCoroutine = null;
        ReloadProgress   = 0f;
        AnimatorUtil.Bool(gunAnimator, reloadingBool, false);
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;
        if (UpgradeManager.JustClosed) return;   // the click/key that picked a card shouldn't shoot
        if (PlayerManagement.Instance != null && PlayerManagement.Instance.IsDead) return;

        if (Input.GetKeyDown(reloadKey))
            TryReload();

        if (autoReloadWhenEmpty && CurrentAmmo <= 0 && !IsReloading)
            TryReload();

        if (Skill_rail.IsAnyCharging) return;   // committed to the railgun shot

        bool wantsFire = holdToFire ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
        if (!wantsFire || IsReloading || Time.time < _nextShotTime) return;

        if (CurrentAmmo > 0)
        {
            Fire();
        }
        else if (Input.GetMouseButtonDown(0))
        {
            SoundManager.Instance?.PlaySound3D("Pistol_Empty", firepos.position);
        }
    }

        void Fire()
        {
            CurrentAmmo--;
            float interval = PlayerManagement.Instance != null ? PlayerManagement.Instance.fireInterval : 0.25f;
            _nextShotTime = Time.time + interval;

            Instantiate(bulletPrefab, firepos.position, transform.rotation);
            AnimatorUtil.Trigger(gunAnimator, fireTrigger);

            SoundManager.Instance?.PlaySound3D("Pistol_Fire", firepos.position);
            if (muzzleLight != null)
            {
                if (_flashCoroutine != null)
                    StopCoroutine(_flashCoroutine);
                _flashCoroutine = StartCoroutine(MuzzleFlashLight());
            }

            OnAmmoChanged?.Invoke();
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

    // -----------------------------------------------------------------
    //  Reload
    // -----------------------------------------------------------------

    /// <summary>Starts a reload if not already reloading and the mag isn't full.</summary>
    public void TryReload()
    {
        if (IsReloading || CurrentAmmo >= MagazineSize || !isActiveAndEnabled) return;
        _reloadCoroutine = StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        ReloadProgress = 0f;
        OnAmmoChanged?.Invoke();

        SoundManager.Instance?.PlaySound3D("Pistol_Reload", transform.position);
        AnimatorUtil.ResetTrigger(gunAnimator, fireTrigger);
        AnimatorUtil.Float(gunAnimator, reloadSpeedFloat, reloadTime > 0f ? reloadClipLength / reloadTime : 1f);
        AnimatorUtil.Bool(gunAnimator, reloadingBool, true);
        AnimatorUtil.Trigger(gunAnimator, reloadTrigger);

        float elapsed = 0f;
        while (elapsed < reloadTime)
        {
            elapsed       += Time.deltaTime;    // pauses automatically when timeScale = 0
            ReloadProgress = Mathf.Clamp01(elapsed / reloadTime);
            yield return null;
        }

        CurrentAmmo      = MagazineSize;
        ReloadProgress   = 0f;
        _reloadCoroutine = null;
        AnimatorUtil.Bool(gunAnimator, reloadingBool, false);
        OnAmmoChanged?.Invoke();
    }

    /// <summary>Instantly fills the magazine (e.g. for pickups).</summary>
    public void RefillMagazine()
    {
        if (_reloadCoroutine != null) { StopCoroutine(_reloadCoroutine); _reloadCoroutine = null; }
        AnimatorUtil.Bool(gunAnimator, reloadingBool, false);
        CurrentAmmo    = MagazineSize;
        ReloadProgress = 0f;
        OnAmmoChanged?.Invoke();
    }

    private void HandleStatsChanged()
    {
        // Magazine upgrade picked → the new bullets are added straight into the current mag.
        int newSize = MagazineSize;
        if (newSize > _lastKnownMagSize) CurrentAmmo += newSize - _lastKnownMagSize;
        _lastKnownMagSize = newSize;
        CurrentAmmo = Mathf.Clamp(CurrentAmmo, 0, newSize);
        OnAmmoChanged?.Invoke();
    }
}
