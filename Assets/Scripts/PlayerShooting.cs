using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;

    public float fireRate = 0.2f;
    private float nextFireTime = 0f;

    public AudioSource audioSource;
    public AudioClip reloadSound;
    public AudioClip shootSound;

    public int maxAmmo = 12;
    private int currentAmmo;

    public float reloadTime = 1.5f;
    public bool isReloading = false;

    public int CurrentAmmo => currentAmmo; 

    void Start()
    {
        currentAmmo = maxAmmo;
    }

    void Update()
    {
        if (isReloading) return;

        
        if (Input.GetKeyDown(KeyCode.R))
        {
            StartCoroutine(Reload());
            return;
        }

        
        if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime)
        {
            if (currentAmmo > 0)
            {
                Shoot();
                currentAmmo--;
                nextFireTime = Time.time + fireRate;

                Debug.Log("Ammo: " + currentAmmo);
            }
            else
            {
                Debug.Log("Out of ammo!");
            }
        }
    }

    void Shoot()
    {
        Vector3 spawnPos = firePoint.position + firePoint.forward * 1f;

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, firePoint.rotation);

        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        Collider playerCol = GetComponent<Collider>();
        Collider bulletCol = bullet.GetComponent<Collider>();

        if (playerCol && bulletCol)
        {
            Physics.IgnoreCollision(playerCol, bulletCol);
        }
    }

    System.Collections.IEnumerator Reload()
    {
        isReloading = true;

        if (audioSource != null && reloadSound != null)
        {
            audioSource.PlayOneShot(reloadSound);
        }

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
        isReloading = false;
    }
}