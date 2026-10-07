using UnityEngine;
using System.Collections.Generic;

public class RailBullet : MonoBehaviour
{
    public LayerMask collisionMask;
    public float speed = 20f;

    [Tooltip("How many walls/obstacles the bullet passes through before being destroyed. Enemies do not count as obstacles!")]
    public int maxPierce = 1;

    private int _pierceCount = 0;
    private Rigidbody2D _rb;
    private TrailRenderer _trail;
    public GameObject[] bloodDecals;
    public GameObject bloodEffectPrefab;

    private HashSet<Enemy_Range> _hitEnemies = new HashSet<Enemy_Range>();
    private HashSet<Collider2D> _hitWalls = new HashSet<Collider2D>();

    void Awake()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySound3D("Railgun_Fire", transform.position);

        _rb = GetComponent<Rigidbody2D>();
        _trail = GetComponentInChildren<TrailRenderer>();
    }

    void FixedUpdate()
    {
        float moveDist = Time.deltaTime * speed;
        transform.Translate(Vector3.up * moveDist);

        RaycastHit2D[] hits = Physics2D.RaycastAll(
            transform.position, transform.up,
            moveDist + 0.1f,
            collisionMask);

        // Sort hits by distance to process closest things first
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null) continue;

            Enemy_Range enemy = hit.collider.GetComponentInParent<Enemy_Range>();
            bool isShield = hit.collider.name == "rw" || hit.collider.gameObject.layer == LayerMask.NameToLayer("Wall");

            if (enemy != null && !isShield)
            {
                // Hit an enemy (and not its shield)
                if (!_hitEnemies.Contains(enemy))
                {
                    _hitEnemies.Add(enemy);
                    if (PlayerManagement.Instance != null)
                        enemy.RegisterHit(PlayerManagement.Instance.dmg * 2);
                    
                    if (bloodEffectPrefab != null)
                        Instantiate(bloodEffectPrefab, hit.point, Quaternion.identity);
                    
                    if (bloodDecals != null && bloodDecals.Length > 0)
                    {
                        int index = Random.Range(0, bloodDecals.Length);
                        Instantiate(bloodDecals[index], hit.point, Quaternion.identity);
                    }
                }
                // Pierces through enemies infinitely! No pierce count logic here.
            }
            else
            {
                // Hit a wall or shield
                if (!_hitWalls.Contains(hit.collider))
                {
                    _hitWalls.Add(hit.collider);
                    if (_pierceCount < maxPierce)
                    {
                        _pierceCount++;
                    }
                    else
                    {
                        StopAndDestroy();
                        return; // Stop entirely
                    }
                }
            }
        }
    }

    void StopAndDestroy()
    {
        speed = 0;
        if (_rb != null) _rb.bodyType = RigidbodyType2D.Static;

        // Detach the trail so it keeps fading independently after the bullet freezes
        if (_trail != null)
        {
            _trail.transform.SetParent(null);   // detach from bullet into the scene
            _trail.emitting = false;             // stop adding new points
            Destroy(_trail.gameObject, _trail.time); // self-destruct once fully faded
        }

        Destroy(gameObject);
    }
}
