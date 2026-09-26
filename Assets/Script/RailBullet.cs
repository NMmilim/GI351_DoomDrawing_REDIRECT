using UnityEngine;

public class RailBullet : MonoBehaviour
{
    public LayerMask collisionMask;
    public float speed = 20f;

    [Tooltip("How many obstacles the bullet passes through before being destroyed.")]
    public int maxPierce = 1;

    private int _pierceCount = 0;
    private Rigidbody2D _rb;
    private TrailRenderer _trail;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _trail = GetComponentInChildren<TrailRenderer>();
    }

    void FixedUpdate()
    {
        transform.Translate(Vector3.up * Time.deltaTime * speed);

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position, transform.up,
            Time.deltaTime * speed + 0.1f,
            collisionMask);

        if (hit.collider == null) return;

        // ── Enemy check ───────────────────────────────────────────────────────
        // GetComponentInParent works whether Enemy_Range is on the collider's
        // GameObject OR on a parent — fixes the pierce-through-enemy bug.
        Enemy_Range enemy = hit.collider.GetComponentInParent<Enemy_Range>();
        bool isEnemy = enemy != null || hit.collider.CompareTag("Enemy");

        if (isEnemy)
        {
            if (enemy != null)
                enemy.RegisterHit(PlayerManagement.Instance.dmg * 2);

            StopAndDestroy();
            return;    // never falls through to pierce logic
        }

        // ── Obstacle / wall ───────────────────────────────────────────────────
        if (_pierceCount < maxPierce)
        {
            _pierceCount++;
            // Push bullet just past the surface so it doesn't re-hit same collider
            transform.position = hit.point + (Vector2)transform.up * 0.15f;
        }
        else
        {
            StopAndDestroy();
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
