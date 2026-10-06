using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player pistol bullet.
///
/// ─── RICOCHET COUNT ─────────────────────────────────────────────────────────
///   Ricochets = (maxBounce - 1) + PlayerManagement.UpgRicochet
///   The prefab uses maxBounce = 2  →  base ricochet = 1 (bounces once, dies on the 2nd wall).
///   Example: 3 Ricochet upgrades → 1 + 3 = 4 bounces.
///
/// ─── HOMING RICOCHET (rare upgrade, PlayerManagement.homingRicochet) ────────
///   • Bullet hits an enemy → instead of disappearing, it flies straight into the
///     nearest OTHER visible enemy (costs 1 ricochet).
///   • Bullet hits a wall   → aims at the nearest visible enemy instead of bouncing
///     by angle. If no enemy is visible, it bounces physically like normal.
///   homingSearchRadius controls how far it looks for the next target.
///
/// ─── NOTES ──────────────────────────────────────────────────────────────────
///   • Enemy detection uses GetComponentInParent, so Enemy_Range can sit on a
///     parent of the collider (same fix RailBullet already had).
///   • lifetime is a safety net so bullets that never hit anything get cleaned up.
/// </summary>
public class Bullet : MonoBehaviour
{
    public LayerMask collisionMask;
    public GameObject bloodEffectPrefab;
    [Tooltip("Wall hits before the bullet dies. 2 = base ricochet of 1. Ricochet upgrades add on top.")]
    public int maxBounce = 1;
    private int bounce = 0;
    public float speed = 20;
    public GameObject[] bloodDecals;

    [Header("Homing ricochet")]
    public float homingSearchRadius = 15f;

    [Header("Safety")]
    public float lifetime = 6f;

    private int          _ricochetsLeft;
    private bool         _homing;
    private Enemy_Range  _ignoreEnemy;     // enemy we just hit — don't hit it again immediately
    private readonly RaycastHit2D[] _hitBuffer = new RaycastHit2D[8];
    private ContactFilter2D _filter;

    void Start()
    {
        PlayerManagement pm = PlayerManagement.Instance;
        int upgrades   = pm != null ? pm.UpgRicochet : 0;
        _homing        = pm != null && pm.homingRicochet;
        _ricochetsLeft = Mathf.Max(0, maxBounce - 1) + upgrades;

        _filter = new ContactFilter2D();
        _filter.SetLayerMask(collisionMask);
        _filter.useTriggers = Physics2D.queriesHitTriggers;

        if (lifetime > 0f) Destroy(gameObject, lifetime);
    }

    void FixedUpdate()
    {
        
        transform.Translate(Vector3.up * Time.deltaTime * speed);
        
        if (!CastNearest(transform.position, transform.up, Time.deltaTime * speed + 0.1f, out RaycastHit2D hit))
            return;

        SoundManager.Instance?.PlaySound3D("Ricochet", transform.position);
        Enemy_Range enemy = hit.collider.GetComponentInParent<Enemy_Range>();

        if (enemy != null)
        {
            HitEnemy(enemy, hit);
            return;
        }

        //if (player != null)
        //{
        //    player.RegisterHit();hj
        //    Destroy(gameObject);
        //    return;
        //}

        HitWall(hit);
    }

    // -----------------------------------------------------------------
    //  Hits
    // -----------------------------------------------------------------

    private void HitEnemy(Enemy_Range enemy, RaycastHit2D hit)
    {
        enemy.RegisterHit(PlayerManagement.Instance != null ? PlayerManagement.Instance.dmg : 1);
        SoundManager.Instance?.PlaySound3D("BulletImpactFlesh", transform.position);
        SpawnBlood(hit.point);

        // Homing: chain into another enemy
        if (_homing && _ricochetsLeft > 0)
        {
            Enemy_Range next = FindHomingTarget(hit.point, enemy);
            if (next != null)
            {
                _ricochetsLeft--;
                bounce++;
                _ignoreEnemy       = enemy;
                transform.position = hit.point;
                FaceTowards(next.transform.position);
                return;
            }
        }

        Destroy(gameObject);
    }

    private void HitWall(RaycastHit2D hit)
    {
        _ignoreEnemy = null;   // hit something else, the last enemy is valid again

        if (_ricochetsLeft <= 0)
        {
            SoundManager.Instance?.PlaySound3D("BulletImpact", transform.position);
            Destroy(gameObject);
            return;
        }

        _ricochetsLeft--;
        bounce++;

        if (_homing)
        {
            Vector2 from = hit.point + hit.normal * 0.05f;
            Enemy_Range target = FindHomingTarget(from, null);
            if (target != null)
            {
                transform.position = from;
                FaceTowards(target.transform.position);
                return;
            }
        }

        // Physical bounce (original behaviour)
        Vector2 reflectDir = Vector2.Reflect(transform.up, hit.normal);
        float angle = Mathf.Atan2(reflectDir.y, reflectDir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    // -----------------------------------------------------------------
    //  Helpers
    // -----------------------------------------------------------------

    /// <summary>Raycast that skips colliders belonging to the enemy we just hit.</summary>
    private bool CastNearest(Vector2 origin, Vector2 dir, float distance, out RaycastHit2D nearest)
    {
        nearest = default;
        int count = Physics2D.Raycast(origin, dir, _filter, _hitBuffer, distance);
        float best = float.MaxValue;
        bool found = false;

        for (int i = 0; i < count; i++)
        {
            RaycastHit2D h = _hitBuffer[i];
            if (h.collider == null) continue;
            if (_ignoreEnemy != null && h.collider.GetComponentInParent<Enemy_Range>() == _ignoreEnemy) continue;
            if (h.distance < best) { best = h.distance; nearest = h; found = true; }
        }
        return found;
    }

    /// <summary>Nearest living enemy (not 'exclude') with clear line of sight from 'from'.</summary>
    private Enemy_Range FindHomingTarget(Vector2 from, Enemy_Range exclude)
    {
        Enemy_Range best = null;
        float bestDist = homingSearchRadius;

        Enemy_Range savedIgnore = _ignoreEnemy;
        _ignoreEnemy = exclude;   // so the LOS ray doesn't hit the enemy we're leaving

        List<Enemy_Range> all = Enemy_Range.Active;
        for (int i = 0; i < all.Count; i++)
        {
            Enemy_Range e = all[i];
            if (e == null || e == exclude || e.IsDead) continue;

            Vector2 to   = e.transform.position;
            float   dist = Vector2.Distance(from, to);
            if (dist > bestDist || dist < 0.01f) continue;

            // Line of sight: first thing we hit must be this enemy (or nothing)
            if (CastNearest(from, (to - from).normalized, dist, out RaycastHit2D h)
                && h.collider.GetComponentInParent<Enemy_Range>() != e)
                continue;

            best = e;
            bestDist = dist;
        }

        _ignoreEnemy = savedIgnore;
        return best;
    }

    private void FaceTowards(Vector3 worldPos)
    {
        Vector2 dir   = ((Vector2)worldPos - (Vector2)transform.position).normalized;
        float   angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void SpawnBlood(Vector2 point)
    {
        Quaternion randomRot = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        if (bloodEffectPrefab != null)
            Instantiate(bloodEffectPrefab, point, randomRot);
        if (bloodDecals != null && bloodDecals.Length > 0)
        {
            int index = Random.Range(0, bloodDecals.Length);
            Instantiate(bloodDecals[index], point, randomRot);
        }
    }
}
