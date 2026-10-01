using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Shooting : MonoBehaviour
{
    public GameObject bulletPrefab;
    [SerializeField] private Light2D muzzleLight;
    [Header("MuzzleLight settings")]
    [SerializeField] private float peakIntensity = 8f;
    [SerializeField] private float flashDuration = 0.08f;

           public Transform firepos;
    private Coroutine _flashCoroutine;
    private void Update()
    {
        if (Time.timeScale == 0f) return;
        if (Input.GetMouseButtonDown(0))
        {
            Shooting();
        }
        void Shooting()
        {
           
            GameObject bullet = Instantiate(bulletPrefab, firepos.position, transform.rotation);

            SoundManager.Instance.PlaySound3D("Pistol_Fire", firepos.position);
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

