using UnityEngine;

/// <summary>
/// Ranged enemy AI — cover-seek, peek-and-fire, and search behaviour.
///
/// DETECTION MODEL
///   The enemy always knows where the player is (found by tag at Start, never lost).
///   "Seeing" the player means having a clear raycast — HasClearShot().
///   CanFire() additionally requires the player to be inside the FOV cone.
///
/// STATE MACHINE
///   SeekCover   → Find and move to the nearest unclaimed CoverPoint.
///   InCover     → Wait behind cover for a random interval, then peek.
///   Peeking     → Physically step out to a peek position.
///                  • If shot is clear  → fire a burst, then Returning.
///                  • If shot not clear → Returning if seen before (shoot last-known),
///                                        else Searching.
///   Returning   → Walk back to cover position, then InCover.
///   Searching   → Slow walk toward player's last-known position.
///                  • If clear shot found  → fire, then SeekCover.
///                  • If last-known reached with no shot → SeekCover.
///
/// SETUP REQUIREMENTS
///   • Script on the enemy root GameObject.
///   • Child GameObject with EnemyFOV (auto-found, optional).
///   • Assign bulletPre, firepos, bloodSplatPrefab.
///   • Player tag must be "Player".
///   • Place CoverPoint components at wall/prop edges in the scene.
///   • Rigidbody2D → Constraints → Freeze Rotation Z = ✓
///   • obstacleMask should include your Wall/Obstruction layer.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy_Range : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  Inspector – References
    // ─────────────────────────────────────────────────────────────

    [Header("References")]
    [Tooltip("Enemy bullet prefab.")]
    public GameObject bulletPre;

    [Tooltip("Muzzle / fire position.")]
    public Transform firepos;

    [Tooltip("Blood splat prefab (Bloodsplat.prefab).")]
    public GameObject bloodSplatPrefab;

    // ─────────────────────────────────────────────────────────────
    //  Inspector – Stats
    // ─────────────────────────────────────────────────────────────

    [Header("Stats")]
    public int maxHp = 3;

    // ─────────────────────────────────────────────────────────────
    //  Inspector – Movement
    // ─────────────────────────────────────────────────────────────

    [Header("Movement")]
    [Tooltip("Speed when running to cover.")]
    public float moveSpeed = 3f;

    [Tooltip("Slow walk speed while searching for the player.")]
    public float searchSpeed = 1.2f;

    [Tooltip("Distance threshold to consider 'arrived' at a position.")]
    public float arrivalThreshold = 0.3f;

    // ─────────────────────────────────────────────────────────────
    //  Inspector – Peek Behaviour
    // ─────────────────────────────────────────────────────────────

    [Header("Peek Behaviour")]
    [Tooltip("How far the enemy steps out from the cover position toward the player. " +
             "Set to 0 to peek in-place.")]
    public float peekOffset = 1.2f;

    [Tooltip("Number of shots to fire per peek before retreating. " +
             "Each shot is separated by firerate seconds.")]
    [Range(1, 8)]
    public int burstCount = 3;

    [Tooltip("Seconds between each shot in the burst.")]
    public float firerate = 0.6f;

    [Tooltip("How long the enemy will wait at the peek position for a clear shot " +
             "before giving up and retreating / searching.")]
    public float peekTimeout = 2.5f;

    [Tooltip("Minimum seconds to wait in cover before peeking again.")]
    public float coverWaitMin = 1.0f;

    [Tooltip("Maximum seconds to wait in cover before peeking again.")]
    public float coverWaitMax = 3.0f;

    [Tooltip("How long the enemy searches before giving up and finding new cover.")]
    public float searchTimeout = 8f;

    [Tooltip("Degrees per second the body rotates to face the player.")]
    public float aimRotationSpeed = 240f;

    // ─────────────────────────────────────────────────────────────
    //  Inspector – Combat
    // ─────────────────────────────────────────────────────────────

    [Header("Combat")]
    [Tooltip("Layer(s) that block bullets — used for the clear-shot raycast.")]
    public LayerMask obstacleMask;

    // ─────────────────────────────────────────────────────────────
    //  Inspector – Blood Splatter
    // ─────────────────────────────────────────────────────────────

    [Header("Blood Splatter")]
    [Range(1, 8)]
    public int splatsPerHit = 3;
    public float splatRadius = 0.6f;

    // ─────────────────────────────────────────────────────────────
    //  Private — State Machine
    // ─────────────────────────────────────────────────────────────

    private enum State { SeekCover, InCover, Peeking, Returning, Searching }
    private State _state = State.SeekCover;

    // ─────────────────────────────────────────────────────────────
    //  Private — References & Core
    // ─────────────────────────────────────────────────────────────

    private int         _hp;
    private Rigidbody2D _rb;
    private EnemyFOV    _fov;
    private Transform   _player;

    // ─────────────────────────────────────────────────────────────
    //  Private — Timers & Counters
    // ─────────────────────────────────────────────────────────────

    private float _stateTimer;       // general countdown for current state
    private float _fireCooldown;     // time between individual shots
    private float _seekCoverTimer;   // prevents getting stuck seeking cover

    // ─────────────────────────────────────────────────────────────
    //  Private — Cover & Peek
    // ─────────────────────────────────────────────────────────────

    private CoverPoint _currentCover;
    private Vector3    _coverPosition;    // world pos of claimed cover slot
    private Vector3    _peekPosition;     // world pos enemy steps out to during peek
    private bool       _atPeekPosition;  // true once the enemy has walked to peekPosition
    private int        _shotsThisPeek;   // shots fired in the current peek burst

    // ─────────────────────────────────────────────────────────────
    //  Private — Player Memory
    // ─────────────────────────────────────────────────────────────

    private bool    _playerEverSeen;    // true once the enemy ever had a clear shot
    private Vector3 _playerLastKnown;   // last world position where we had clear LOS

    // ─────────────────────────────────────────────────────────────
    //  Static — Team Alert
    //  When any one enemy fires, all teammates in InCover skip
    //  their wait timer and peek immediately.
    // ─────────────────────────────────────────────────────────────

    private static float _teamAlertTimer = 0f;

    /// <summary>Called when any Enemy_Range fires a shot.</summary>
    private static void BroadcastAlert()
    {
        _teamAlertTimer = 6f;   // team stays alerted for 6 seconds after last shot
    }

    private static bool TeamIsAlerted => _teamAlertTimer > 0f;

    // ─────────────────────────────────────────────────────────────
    //  Unity Messages
    // ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rb  = GetComponent<Rigidbody2D>();
        _fov = GetComponentInChildren<EnemyFOV>();
    }

    private void Start()
    {
        _hp = maxHp;

        GameObject playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
        {
            _player = playerGO.transform;
            if (_fov != null) _fov._playerRef = _player;
        }
        else
        {
            Debug.LogWarning($"[Enemy_Range] '{name}': No GameObject with tag 'Player' found!");
        }

        // Small random delay so multiple enemies stagger their behaviour
        Invoke(nameof(EnterSeekCover), Random.Range(0f, 0.5f));
    }

    private void Update()
    {
        if (_player == null) return;

        // Tick team-alert countdown
        if (_teamAlertTimer > 0f)
            _teamAlertTimer -= Time.deltaTime;

        // Rotate body: track player only after we've seen them.
        // Before first detection, face the direction of movement instead.
        UpdateBodyRotation();

        if (_fireCooldown > 0f)
            _fireCooldown -= Time.deltaTime;

        switch (_state)
        {
            case State.SeekCover:  TickSeekCover();  break;
            case State.InCover:    TickInCover();    break;
            case State.Peeking:    TickPeeking();    break;
            case State.Returning:  TickReturning();  break;
            case State.Searching:  TickSearching();  break;
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  State: SeekCover
    //  Find the nearest unclaimed CoverPoint and run to it.
    // ─────────────────────────────────────────────────────────────

    private void EnterSeekCover()
    {
        _state          = State.SeekCover;
        _seekCoverTimer = 3.5f;

        if (_currentCover != null) { _currentCover.Release(this); _currentCover = null; }

        CoverPoint best = CoverPoint.FindBestCover(transform.position, this);
        if (best != null)
        {
            _currentCover  = best;
            _currentCover.Claim(this);
            _coverPosition = _currentCover.transform.position;
        }
        else
        {
            // No cover available — fight from current position
            _coverPosition = transform.position;
            EnterInCover();
        }
    }

    private void TickSeekCover()
    {
        _seekCoverTimer -= Time.deltaTime;
        if (_seekCoverTimer <= 0f)
        {
            // Couldn't reach cover (wall in the way etc.) — fight from here
            _rb.linearVelocity = Vector2.zero;
            _coverPosition     = transform.position;
            if (_currentCover != null) { _currentCover.Release(this); _currentCover = null; }
            EnterInCover();
            return;
        }

        Vector2 toTarget = (Vector2)_coverPosition - _rb.position;
        if (toTarget.magnitude <= arrivalThreshold)
        {
            _rb.linearVelocity = Vector2.zero;
            EnterInCover();
            return;
        }

        _rb.linearVelocity = toTarget.normalized * moveSpeed;
    }

    // ─────────────────────────────────────────────────────────────
    //  State: InCover
    //  Wait behind cover. FOV beam hidden. After timer → peek.
    // ─────────────────────────────────────────────────────────────

    private void EnterInCover()
    {
        _state             = State.InCover;
        _rb.linearVelocity = Vector2.zero;
        _stateTimer        = Random.Range(coverWaitMin, coverWaitMax);
        SetFOVVisible(false);
    }

    private void TickInCover()
    {
        _stateTimer -= Time.deltaTime;

        // If a teammate is already shooting, skip the wait and peek now.
        // This creates coordinated simultaneous peeking across all enemies.
        if (_stateTimer <= 0f || TeamIsAlerted)
            EnterPeeking();
    }

    // ─────────────────────────────────────────────────────────────
    //  State: Peeking
    //  Step OUT from cover toward player. Fire a burst if shot is clear.
    //  If shot never clears → search (or fire at last known if seen before).
    // ─────────────────────────────────────────────────────────────

    private void EnterPeeking()
    {
        _state           = State.Peeking;
        _stateTimer      = peekTimeout;
        _shotsThisPeek   = 0;
        _atPeekPosition  = false;

        // Calculate peek position: step out from cover toward player's last known location
        Vector3 target   = _playerEverSeen ? _playerLastKnown : _player.position;
        Vector3 dir      = (target - _coverPosition).normalized;
        _peekPosition    = _coverPosition + dir * peekOffset;

        SetFOVVisible(true);
    }

    private void TickPeeking()
    {
        _stateTimer -= Time.deltaTime;

        // ── Phase 1: Walk to the peek position ──────────────────
        if (!_atPeekPosition)
        {
            Vector2 toSpot = (Vector2)_peekPosition - _rb.position;
            if (toSpot.magnitude > arrivalThreshold)
            {
                _rb.linearVelocity = toSpot.normalized * moveSpeed;
                return;
            }
            // Arrived at peek position
            _rb.linearVelocity = Vector2.zero;
            _atPeekPosition    = true;
        }

        // ── Phase 2: Fire burst while at peek position ───────────
        if (_fireCooldown <= 0f && CanFire())
        {
            // Update last-known position when we actually get a clear shot
            _playerLastKnown = _player.position;
            _playerEverSeen  = true;

            Shoot();
            _fireCooldown = firerate;
            _shotsThisPeek++;

            // Burst complete — duck back
            if (_shotsThisPeek >= burstCount)
            {
                EnterReturning();
                return;
            }
        }

        // ── Phase 3: Peek timer expired ─────────────────────────
        if (_stateTimer <= 0f)
        {
            if (_playerEverSeen)
            {
                // Seen the player before — fire a speculative shot at last-known position
                // even though we can't see them right now
                ShootAt(_playerLastKnown);
                EnterReturning();
            }
            else
            {
                // Never seen the player — go searching
                EnterSearching();
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  State: Returning
    //  Walk back to the cover position. FOV beam hidden.
    // ─────────────────────────────────────────────────────────────

    private void EnterReturning()
    {
        _state = State.Returning;
        SetFOVVisible(false);
    }

    private void TickReturning()
    {
        Vector2 toTarget = (Vector2)_coverPosition - _rb.position;
        if (toTarget.magnitude <= arrivalThreshold)
        {
            _rb.linearVelocity = Vector2.zero;
            EnterInCover();
            return;
        }
        _rb.linearVelocity = toTarget.normalized * moveSpeed;
    }

    // ─────────────────────────────────────────────────────────────
    //  State: Searching
    //  Enemy never saw the player from cover. Slowly walks toward
    //  last-known position (or player position) looking for them.
    //  FOV beam is active. If clear shot found → fire → seek cover.
    //  If last-known reached and still no shot → seek new cover.
    // ─────────────────────────────────────────────────────────────

    private void EnterSearching()
    {
        // If this enemy has never established line of sight, there's no
        // last-known position to walk toward — just return to cover.
        if (!_playerEverSeen)
        {
            EnterSeekCover();
            return;
        }

        _state      = State.Searching;
        _stateTimer = searchTimeout;
        SetFOVVisible(true);
    }

    private void TickSearching()
    {
        _stateTimer -= Time.deltaTime;

        // Spotted the player while searching — update last-known and seek cover.
        // Do NOT fire immediately while out in the open.
        if (HasClearShot())
        {
            _playerLastKnown = _player.position;
            _playerEverSeen  = true;
            _rb.linearVelocity = Vector2.zero;
            EnterSeekCover();   // find cover, then peek-and-fire from safety
            return;
        }

        // Walk toward the LAST KNOWN position (a fixed snapshot).
        // Never use live _player.position here — that would beeline straight to the player.
        Vector2 toTarget = (Vector2)_playerLastKnown - _rb.position;

        if (toTarget.magnitude <= arrivalThreshold || _stateTimer <= 0f)
        {
            // Reached last-known location (player wasn't there) or timed out.
            // Give up and find new cover to try again.
            _rb.linearVelocity = Vector2.zero;
            EnterSeekCover();
            return;
        }

        _rb.linearVelocity = toTarget.normalized * searchSpeed;
    }

    // ─────────────────────────────────────────────────────────────
    //  Combat
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Controls body rotation based on awareness state.
    /// • Before seeing the player: face the direction of movement (natural walk).
    /// • After seeing the player at least once: always rotate to face last-known position.
    /// • While peeking (FOV active): track live player position for aiming.
    /// </summary>
    private void UpdateBodyRotation()
    {
        float targetAngle;

        if (_playerEverSeen || _state == State.Peeking)
        {
            // Track the player (live position when peeking, last-known otherwise)
            Vector3 lookTarget = (_state == State.Peeking && _player != null)
                                 ? _player.position
                                 : _playerLastKnown;
            Vector2 dir   = ((Vector2)lookTarget - _rb.position).normalized;
            targetAngle   = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        }
        else
        {
            // Never seen player yet — face movement direction
            if (_rb.linearVelocity.sqrMagnitude > 0.05f)
                targetAngle = Mathf.Atan2(_rb.linearVelocity.y, _rb.linearVelocity.x) * Mathf.Rad2Deg;
            else
                return;   // stationary and unaware — keep current rotation
        }

        float newAngle = Mathf.MoveTowardsAngle(
                             transform.eulerAngles.z, targetAngle,
                             aimRotationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
    }

    /// <summary>Direct aim at player — kept for internal use during burst fire.</summary>
    private void AimBodyAtPlayer()
    {
        Vector2 dir      = ((Vector2)_player.position - _rb.position).normalized;
        float   angle    = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float   newAngle = Mathf.MoveTowardsAngle(
                               transform.eulerAngles.z, angle,
                               aimRotationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
    }

    /// <summary>
    /// Returns true when the player is inside the narrow FOV cone AND the
    /// shot is unobstructed. Falls back to a plain raycast if FOV is absent.
    /// </summary>
    private bool CanFire()
    {
        if (_player == null) return false;
        if (_fov != null && _fov.enabled)
            return _fov.IsInFireCone(_player.position);
        return HasClearShot();
    }

    /// <summary>Plain obstacle raycast — no cone angle restriction.</summary>
    private bool HasClearShot()
    {
        if (_player == null || firepos == null) return false;
        Vector2 origin = firepos.position;
        Vector2 dir    = ((Vector2)_player.position - origin).normalized;
        float   dist   = Vector2.Distance(origin, _player.position);
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, dist, obstacleMask);
        return hit.collider == null;
    }

    /// <summary>Fire one bullet at the player's current position.</summary>
    private void Shoot()
    {
        if (bulletPre == null || firepos == null || _player == null) return;
        ShootAt(_player.position);
    }

    /// <summary>Fire one bullet toward an arbitrary world position (e.g. last-known).</summary>
    private void ShootAt(Vector3 targetPos)
    {
        if (bulletPre == null || firepos == null) return;
        Vector2 shootDir = ((Vector2)targetPos - (Vector2)firepos.position).normalized;
        float   angle    = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg;
        // EnemyBullet moves itself via transform.Translate — do NOT set rigidbody velocity.
        Instantiate(bulletPre, firepos.position, Quaternion.Euler(0f, 0f, angle));

        // Alert all teammates — enemies waiting in cover will skip their wait and peek now
        BroadcastAlert();
    }

    // ─────────────────────────────────────────────────────────────
    //  Damage / Death
    // ─────────────────────────────────────────────────────────────

    /// <summary>Called when this enemy is hit. Spawns blood splatter decals.</summary>
    public void RegisterHit(int amount)
    {
        if (bloodSplatPrefab != null)
        {
            for (int i = 0; i < splatsPerHit; i++)
            {
                Vector2 offset   = Random.insideUnitCircle * splatRadius;
                Vector3 splatPos = transform.position + new Vector3(offset.x, offset.y, 0f);
                float   rot      = Random.Range(0f, 360f);
                Instantiate(bloodSplatPrefab, splatPos, Quaternion.Euler(0f, 0f, rot));
            }
        }

        _hp -= amount;
        if (_hp <= 0)
        {
            if (_currentCover != null) _currentCover.Release(this);
            Destroy(gameObject);
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Utilities
    // ─────────────────────────────────────────────────────────────

    private void SetFOVVisible(bool visible)
    {
        if (_fov != null)
            _fov.enabled = visible;
    }

    private void OnDestroy()
    {
        if (_currentCover != null)
            _currentCover.Release(this);
    }
}
