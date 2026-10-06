using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// OPTIONAL pistol ammo display. Use only the parts your HUD has; everything is optional.
///
///   ammoText       : TMP text → "6 / 8"  (format editable, {0}=current {1}=magazine)
///   reloadingLabel : any object (e.g. "RELOADING" text) shown only while reloading
///   reloadFill     : Filled Image (Horizontal or Radial) that fills 0→1 during a reload
///   bulletIcons    : bullet Images in a row; extra ones are hidden, spent ones are dimmed.
///                    Add as many as the max magazine you expect (e.g. 30) — unused hide.
///
///   'shooting' is found automatically if left empty.
/// </summary>
public class AmmoUI : MonoBehaviour
{
    public Shooting shooting;

    public TMP_Text   ammoText;
    public string     ammoFormat = "{0} / {1}";
    public GameObject reloadingLabel;
    public Image      reloadFill;

    [Header("Optional bullet icons")]
    public Image[] bulletIcons;
    public Color   loadedColor = Color.white;
    public Color   spentColor  = new Color(1f, 1f, 1f, 0.2f);

    private void Start()
    {
        if (shooting == null) shooting = FindAnyObjectByType<Shooting>();
    }

    private void Update()
    {
        if (shooting == null) return;

        int  ammo      = shooting.CurrentAmmo;
        int  mag       = shooting.MagazineSize;
        bool reloading = shooting.IsReloading;

        if (ammoText != null)       ammoText.text = string.Format(ammoFormat, ammo, mag);
        if (reloadingLabel != null) reloadingLabel.SetActive(reloading);
        if (reloadFill != null)
        {
            reloadFill.enabled    = reloading;
            reloadFill.fillAmount = shooting.ReloadProgress;
        }

        if (bulletIcons != null)
        {
            for (int i = 0; i < bulletIcons.Length; i++)
            {
                if (bulletIcons[i] == null) continue;
                bool exists = i < mag;
                bulletIcons[i].gameObject.SetActive(exists);
                if (exists) bulletIcons[i].color = i < ammo ? loadedColor : spentColor;
            }
        }
    }
}
