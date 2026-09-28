using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;
public class Skill_ : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firepos;
    [SerializeField] private Light2D muzzleLight;
    public GameObject RailBullet;
    private Coroutine _flashCoroutine;
    [Header("MuzzleLight settings")]
    [SerializeField] float peakIntensity = 8f;
    [SerializeField] float flashDuration = 0.08f;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha2)&&Time.time >= PlayerManagement.Instance.nextFireTime)
        {
            RailGun();
             PlayerManagement.Instance.nextFireTime = Time.time + (PlayerManagement.Instance.cooldown*3);
        }
        void RailGun()
        {
            RailBullet = Instantiate(bulletPrefab, firepos.position, firepos.rotation);
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
}

