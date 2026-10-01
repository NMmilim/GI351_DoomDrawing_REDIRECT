using UnityEngine;
using Pathfinding;   // A* Pathfinding Project

/// <summary>
/// Ranged enemy AI using A* Pathfinding Project (AIPath) for 2D top-down movement.
///
/// WHY A* PATHFINDING PROJECT instead of Unity NavMesh?
///   Unity's built-in NavMesh is a 3D system. It cannot read Collider2D components,
///   so 2D walls are invisible to the baker regardless of any tricks applied.
///   A* Pathfinding Project has native 2D support — it reads Collider2D directly,
///   wall obstacles are detected automatically, and no 3D workarounds are needed.
///
/// REQUIRED PACKAGE
///   Download the free version at: https://arongranberg.com/astar
///   Import the .unitypackage, then add an AstarPath component to a scene object
///   and configure a Grid Graph (see SETUP REQUIREMENTS below).
///
/// STATE MACHINE
///   SeekCover    - Path to the nearest unclaimed CoverPoint.
///   InCover      - Wait behind cover; FOV hidden. After timer -> Peeking.
///   Peeking      - Step out from cover.
///                   Phase 1: Walk to peek position.
///                   Phase 2: Noticing delay (noticingTime of clear LOS before firing).
///                   Phase 3: Fire burst. On burst complete -> Returning.
///   Returning    - Walk back to cover -> InCover.
///   Roaming      - Slow walk, sweeping the map. Triggered after lostSightRoamDelay
///                  seconds of no LOS, or spawnRoamDelay seconds after spawn with
///                  no detection at all.
///
/// SETUP REQUIREMENTS
///   Scene:
///     1. Create empty GameObject "Pathfinder".
///     2. Add Component: AstarPath.
///     3. Graphs -> + -> Grid Graph.
///        - Width/Depth: cover your whole map (e.g. 40 x 40).
///        - Node Size: 0.5 (smaller = more accurate).
///        - Use 2D Physics: checked.
///        - Obstacle Layer Mask: Wall (your wall layer).
///     4. Click Scan. Open space = blue nodes, walls = red/no nodes.
///
///   Enemy Prefab:
///     - AIPath component (Add Component -> Pathfinding -> AIPath).
///       Set: Enable Rotation = false, Gravity = 0.
///     - Seeker component (auto-added with AIPath).
///     - Enemy_Range component (this script).
///     - EnemyFOV on a child GameObject (optional, but recommended).
///     - Assign bulletPre, firepos, bloodSplatPrefab in Inspector.
///     - Player tag must be "Player".
/// </summary>
[RequireComponent(typeof(AIPath))]
[RequireComponent(typeof(Seeker))]
public class Enemy_Range : MonoBehaviour
{
    // -----------------------------------------------------------------
    //  Inspector - References
    // -----------------------------------------------------------------

    [Header("References")]
    [Tooltip("Enemy bullet prefab.")]
    public GameObject bulletPre;

    [Tooltip("Muzzle / fire position.")]
    public Transform firepos;

    [Tooltip("Blood splat prefab.")]
    public GameObject bloodSplatPrefab;

    // -----------------------------------------------------------------
    //  Inspector - Stats
    // -----------------------------------------------------------------

    [Header("Stats")]
    public int maxHp = 3;

    // -----------------------------------------------------------------
    //  Inspector - Movement
    // -----------------------------------------------------------------

    [Header("Movement")]
    [Tooltip("Speed when running to cover or the peek position.")]
    public float moveSpeed = 3f;

    [Tooltip("Slower speed used only during Roaming state — simulates cautious walking.")]
    public float roamSpeed = 1.2f;

    [Tooltip("Distance threshold to consider 'arrived' at a destination.")]
    public float arrivalThreshold = 0.35f;

    // -----------------------------------------------------------------
    //  Inspector - Peek Behaviour
    // -----------------------------------------------------------------

    [Header("Peek Behaviour")]
    [Tooltip("How far the enemy steps out from the cover position toward the player.")]
    public float peekOffset = 1.2f;

    [Tooltip("Number of shots per peek burst before retreating.")]
    [Range(1, 8)]
    public int burstCount = 3;

    [Tooltip("Seconds between each shot in the burst.")]
    public float firerate = 0.6f;

    [Tooltip("Maximum seconds to wait at peek position for a clear shot before retreating.")]
    public float peekTimeout = 2.5f;

    [Tooltip("Minimum seconds to wait in cover before peeking again.")]
    public float coverWaitMin = 1.0f;

    [Tooltip("Maximum seconds to wait in cover before peeking again.")]
    public float coverWaitMax = 3.0f;

    [Tooltip("Degrees per second the body rotates toward the aim direction.")]
    public float aimRotationSpeed = 240f;

    // -----------------------------------------------------------------
    //  Inspector - Detection / Noticing
    // -----------------------------------------------------------------

    [Header("Detection / Noticing")]
    [Tooltip("Seconds of continuous clear LOS required before the enemy reacts and fires.")]
    public float noticingTime = 0.6f;

    [Tooltip("Seconds of lost LOS before switching to Roaming.")]
    public float lostSightRoamDelay = 20f;

    [Tooltip("Seconds after spawn without any detection before starting to Roam.")]
    public float spawnRoamDelay = 10f;

    [Tooltip("Radius (world units) in which detected player location is broadcast to allies.")]
    public float alertBroadcastRadius = 15f;

    // -----------------------------------------------------------------
    //  Inspector - Roaming
    // -----------------------------------------------------------------

    [Header("Roaming")]
    [Tooltip("How long the enemy pauses at each roam waypoint.")]
    public float roamWaypointPause = 1.5f;

    [Tooltip("Max distance for picking a random roam waypoint.")]
    public float roamWaypointRadius = 8f;

    // -----------------------------------------------------------------
    //  Inspector - Stuck Recovery
    // -----------------------------------------------------------------

    [Header("Stuck Recovery")]
    [Tooltip("If the enemy doesn't reach its destination within this many seconds it will abandon the path and re-route.")]
    public float stuckTimeout = 10f;

    [Tooltip("Minimum distance the enemy must travel per stuckTimeout window to be considered not stuck.")]
    public float stuckMoveThreshold = 0.15f;

    // -----------------------------------------------------------------
    //  Inspector - Combat
    // -----------------------------------------------------------------

    [Header("Combat")]
    [Tooltip("Layers that block bullets — used for the clear-shot raycast.")]
    public LayerMask obstacleMask;

    // -----------------------------------------------------------------
    //  Inspector - Blood Splatter
    // -----------------------------------------------------------------

    [Header("Blood Splatter")]
    [Range(1, 8)]
    public int splatsPerHit = 3;
    public float splatRadius = 0.6f;

    // -----------------------------------------------------------------
    //  State Machine
    // -----------------------------------------------------------------

    private enum State
    {
        SeekCover,
        InCover,
        Peeking,
        Returning,
        Roaming
    }

    private State _state = State.SeekCover;

    // -----------------------------------------------------------------
    //  Private - References
    // -----------------------------------------------------------------

    private int      _hp;
    private AIPath   _ai;
    private EnemyFOV _fov;
    private Transform _player;

    // -----------------------------------------------------------------
    //  Private - Timers
    // -----------------------------------------------------------------

    private float _stateTimer;
    private float _fireCooldown;
    private float _seekCoverTimer;
    private float _noticingTimer;
    private float _lostSightTimer;
    private float _spawnTimer;
    private float _roamWaypointTimer;

    // Stuck detection
    private float   _stuckTimer;
    private Vector3 _stuckLastPos;

    // -----------------------------------------------------------------
    //  Private - Cover & Peek
    // -----------------------------------------------------------------

    private CoverPoint _currentCover;
    private Vector3    _coverPosition;
    private Vector3    _peekPosition;
    private bool       _atPeekPosition;
    private int        _shotsThisPeek;
    private bool       _noticed;

    // -----------------------------------------------------------------
    //  Private - Player Memory
    // -----------------------------------------------------------------

    private bool    _playerEverSeen;
    private Vector3 _playerLastKnown;
    private bool    _haveLastKnown;

    // -----------------------------------------------------------------
    //  Private - Spawn gate
    // -----------------------------------------------------------------

    private bool _spawnRoamTriggered;

    // -----------------------------------------------------------------
    //  Static - Team Alert
    // -----------------------------------------------------------------

    private static float _teamAlertTimer = 0f;

    private static void BroadcastTeamAlert() => _teamAlertTimer = 6f;

    private static bool TeamIsAlerted => _teamAlertTimer > 0f;

    // -----------------------------------------------------------------
    //  Unity Messages
    // -----------------------------------------------------------------

    private void Awake()
    {
        _ai  = GetComponent<AIPath>();
        _fov = GetComponentInChildren<EnemyFOV>();

        // 2D configuration for AIPath
        _ai.enableRotation = false;   // we rotate the body ourselves
        _ai.gravity        = Vector3.zero;
        _ai.orientation    = OrientationMode.YAxisForward;
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
            Debug.LogWarning($"[Enemy_Range] '{name}': No 'Player' tagged object found.");
        }

        _spawnTimer         = 0f;
        _spawnRoamTriggered = false;
        _lostSightTimer     = 0f;

        Invoke(nameof(EnterSeekCover), Random.Range(0f, 0.5f));
    }

    private void Update()
    {
        if (_player == null) return;

        if (_teamAlertTimer > 0f)  _teamAlertTimer  -= Time.deltaTime;
        if (_fireCooldown   > 0f)  _fireCooldown    -= Time.deltaTime;

        // ---- Spawn-roam gate ---------------------------------------
        if (!_spawnRoamTriggered && !_playerEverSeen)
        {
            _spawnTimer += Time.deltaTime;
            if (_spawnTimer >= spawnRoamDelay)
            {
                _spawnRoamTriggered = true;
                EnterRoaming();
                return;
            }
        }

        // ---- Lost-sight-roam gate ----------------------------------
        if (_playerEverSeen && _state != State.Roaming)
        {
            if (HasClearShot())
                _lostSightTimer = 0f;
            else
            {
                _lostSightTimer += Time.deltaTime;
                if (_lostSightTimer >= lostSightRoamDelay)
                {
                    _lostSightTimer = 0f;
                    EnterRoaming();
                    return;
                }
            }
        }

        UpdateBodyRotation();

        switch (_state)
        {
            case State.SeekCover:  TickSeekCover();  break;
            case State.InCover:    TickInCover();    break;
            case State.Peeking:    TickPeeking();    break;
            case State.Returning:  TickReturning();  break;
            case State.Roaming:    TickRoaming();    break;
        }
    }

    // -----------------------------------------------------------------
    //  State: SeekCover
    // -----------------------------------------------------------------

    private void EnterSeekCover()
    {
        _state          = State.SeekCover;
        _seekCoverTimer = 4f;
        ResetStuckTimer();

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
            _coverPosition = transform.position;
            EnterInCover();
            return;
        }

        SetSpeed(moveSpeed);
        SetDestination(_coverPosition);
    }

    private void TickSeekCover()
    {
        _seekCoverTimer -= Time.deltaTime;
        if (_seekCoverTimer <= 0f || HasArrived())
        {
            StopMoving();
            if (_seekCoverTimer <= 0f) _coverPosition = transform.position;
            EnterInCover();
            return;
        }
        CheckStuck();
    }

    // -----------------------------------------------------------------
    //  State: InCover
    // -----------------------------------------------------------------

    private void EnterInCover()
    {
        _state      = State.InCover;
        _stateTimer = Random.Range(coverWaitMin, coverWaitMax);

        StopMoving();
        SetFOVVisible(false);

        _noticingTimer  = 0f;
        _lostSightTimer = 0f;
    }

    private void TickInCover()
    {
        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f || TeamIsAlerted)
            EnterPeeking();
    }

    // -----------------------------------------------------------------
    //  State: Peeking
    //  Phase 1 - Walk to peek spot.
    //  Phase 2 - Noticing delay (noticingTime of clear LOS before firing).
    //  Phase 3 - Fire burst.
    // -----------------------------------------------------------------

    private void EnterPeeking()
    {
        _state          = State.Peeking;
        _stateTimer     = peekTimeout;
        _shotsThisPeek  = 0;
        _atPeekPosition = false;
        _noticingTimer  = 0f;
        _noticed        = false;

        Vector3 target   = _haveLastKnown ? _playerLastKnown : _player.position;
        Vector3 dir      = (target - _coverPosition).normalized;
        _peekPosition    = _coverPosition + dir * peekOffset;

        SetSpeed(moveSpeed);
        SetDestination(_peekPosition);
        SetFOVVisible(true);
    }

    private void TickPeeking()
    {
        _stateTimer -= Time.deltaTime;

        // Phase 1: walk to peek spot
        if (!_atPeekPosition)
        {
            if (!HasArrived()) return;
            StopMoving();
            _atPeekPosition = true;
        }

        // Phase 2: noticing delay
        if (!_noticed)
        {
            if (HasClearShot())
            {
                _noticingTimer += Time.deltaTime;
                if (_noticingTimer >= noticingTime)
                {
                    _noticed         = true;
                    _playerLastKnown = _player.position;
                    _playerEverSeen  = true;
                    _haveLastKnown   = true;
                    AlertNearbyAllies(_playerLastKnown);
                    BroadcastTeamAlert();
                    _spawnRoamTriggered = true;
                    _lostSightTimer     = 0f;
                    TakeShot();
                }
            }
            else
            {
                _noticingTimer = 0f;
            }
        }
        else
        {
            // Phase 3: fire burst
            if (_fireCooldown <= 0f && CanFire())
            {
                _playerLastKnown = _player.position;
                TakeShot();
                if (_shotsThisPeek >= burstCount)
                {
                    EnterReturning();
                    return;
                }
            }
        }

        // Timeout
        if (_stateTimer <= 0f)
        {
            if (_haveLastKnown) ShootAt(_playerLastKnown);
            EnterReturning();
        }
    }

    // -----------------------------------------------------------------
    //  State: Returning
    // -----------------------------------------------------------------

    private void EnterReturning()
    {
        _state = State.Returning;
        StopMoving();
        SetFOVVisible(false);
        SetSpeed(moveSpeed);
        SetDestination(_coverPosition);
        ResetStuckTimer();
    }

    private void TickReturning()
    {
        if (HasArrived())
        {
            StopMoving();
            EnterInCover();
            return;
        }
        CheckStuck();
    }

    // -----------------------------------------------------------------
    //  State: Roaming
    // -----------------------------------------------------------------

    private void EnterRoaming()
    {
        _state             = State.Roaming;
        _roamWaypointTimer = 0f;
        _lostSightTimer    = 0f;

        StopMoving();
        SetFOVVisible(true);
        SetSpeed(roamSpeed);
        ResetStuckTimer();
        PickRoamWaypoint();
    }

    private void TickRoaming()
    {
        if (HasClearShot())
        {
            _playerLastKnown    = _player.position;
            _playerEverSeen     = true;
            _haveLastKnown      = true;
            _spawnRoamTriggered = true;
            _lostSightTimer     = 0f;
            AlertNearbyAllies(_playerLastKnown);
            BroadcastTeamAlert();
            StopMoving();
            EnterSeekCover();
            return;
        }

        if (HasArrived())
        {
            StopMoving();
            _roamWaypointTimer += Time.deltaTime;
            if (_roamWaypointTimer >= roamWaypointPause)
            {
                _roamWaypointTimer = 0f;
                ResetStuckTimer();
                PickRoamWaypoint();
            }
        }
        else
        {
            CheckStuck();
        }
    }

    private void PickRoamWaypoint()
    {
        // A* handles validity automatically — just pick a random nearby point.
        Vector2 rand   = Random.insideUnitCircle * roamWaypointRadius;
        Vector3 target = transform.position + new Vector3(rand.x, rand.y, 0f);
        SetSpeed(roamSpeed);
        SetDestination(target);
    }

    // -----------------------------------------------------------------
    //  Stuck Detection
    // -----------------------------------------------------------------

    private void ResetStuckTimer()
    {
        _stuckTimer   = 0f;
        _stuckLastPos = transform.position;
    }

    /// <summary>
    /// Call every frame while the enemy is actively trying to reach a destination.
    /// If the enemy hasn't moved stuckMoveThreshold units in stuckTimeout seconds
    /// it abandons its current goal and re-routes.
    /// </summary>
    private void CheckStuck()
    {
        _stuckTimer += Time.deltaTime;
        if (_stuckTimer < stuckTimeout) return;

        // Check displacement over the timeout window
        float moved = Vector3.Distance(transform.position, _stuckLastPos);
        if (moved < stuckMoveThreshold)
        {
            // Genuinely stuck — abandon current goal
            HandleStuck();
        }

        // Reset regardless so we don't spam every frame
        ResetStuckTimer();
    }

    private void HandleStuck()
    {
        StopMoving();

        // Release claimed cover so another enemy can use it
        if (_currentCover != null) { _currentCover.Release(this); _currentCover = null; }

        switch (_state)
        {
            case State.SeekCover:
            case State.Returning:
                // Can't reach cover — take cover wherever we are and try fresh cover next peek cycle
                _coverPosition = transform.position;
                EnterInCover();
                break;

            case State.Roaming:
                // Pick a different random waypoint and keep roaming
                ResetStuckTimer();
                PickRoamWaypoint();
                break;
        }
    }

    // -----------------------------------------------------------------
    //  Alert Broadcast
    // -----------------------------------------------------------------

    private void AlertNearbyAllies(Vector3 playerPos)
    {
        Enemy_Range[] all = FindObjectsByType<Enemy_Range>(FindObjectsSortMode.None);
        foreach (Enemy_Range ally in all)
        {
            if (ally == this) continue;
            if (Vector3.Distance(transform.position, ally.transform.position) > alertBroadcastRadius)
                continue;
            ally.ReceiveAlert(playerPos);
        }
    }

    /// <summary>Called by a teammate who detected the player.</summary>
    public void ReceiveAlert(Vector3 knownPlayerPos)
    {
        _playerLastKnown    = knownPlayerPos;
        _haveLastKnown      = true;
        _playerEverSeen     = true;
        _spawnRoamTriggered = true;
        _lostSightTimer     = 0f;

        if (_state == State.InCover)   EnterPeeking();
        else if (_state == State.Roaming) { StopMoving(); EnterSeekCover(); }
    }

    // -----------------------------------------------------------------
    //  Combat
    // -----------------------------------------------------------------

    private void UpdateBodyRotation()
    {
        float targetAngle;

        if (_haveLastKnown || _state == State.Peeking)
        {
            Vector3 look = (_state == State.Peeking && _player != null)
                           ? _player.position
                           : _playerLastKnown;
            Vector2 dir  = ((Vector2)look - (Vector2)transform.position).normalized;
            targetAngle  = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        }
        else
        {
            Vector3 vel = _ai.velocity;
            if (vel.sqrMagnitude > 0.05f)
                targetAngle = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg;
            else
                return;
        }

        float newAngle = Mathf.MoveTowardsAngle(
                             transform.eulerAngles.z, targetAngle,
                             aimRotationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
    }

    private bool CanFire()
    {
        if (_player == null) return false;
        if (_fov != null && _fov.enabled) return _fov.IsInFireCone(_player.position);
        return HasClearShot();
    }

    private bool HasClearShot()
    {
        if (_player == null || firepos == null) return false;
        Vector2 origin = firepos.position;
        Vector2 dir    = ((Vector2)_player.position - origin).normalized;
        float   dist   = Vector2.Distance(origin, _player.position);
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, dist, obstacleMask);
        return hit.collider == null;
    }

    private void TakeShot()
    {
        ShootAt(_player.position);
        _fireCooldown = firerate;
        _shotsThisPeek++;
    }

    private void ShootAt(Vector3 targetPos)
    {
        if (bulletPre == null || firepos == null) return;
        Vector2 dir   = ((Vector2)targetPos - (Vector2)firepos.position).normalized;
        float   angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Instantiate(bulletPre, firepos.position, Quaternion.Euler(0f, 0f, angle));
    }

    // -----------------------------------------------------------------
    //  Damage / Death
    // -----------------------------------------------------------------

    public void RegisterHit(int amount)
    {
        if (bloodSplatPrefab != null)
        {
            for (int i = 0; i < splatsPerHit; i++)
            {
                Vector2 offset   = Random.insideUnitCircle * splatRadius;
                Vector3 splatPos = transform.position + new Vector3(offset.x, offset.y, 0f);
                Instantiate(bloodSplatPrefab, splatPos, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
            }
        }

        _hp -= amount;
        if (_hp <= 0)
        {
            if (_currentCover != null) _currentCover.Release(this);
            Destroy(gameObject);
        }
    }

    // -----------------------------------------------------------------
    //  AIPath Helpers
    // -----------------------------------------------------------------

    private void SetDestination(Vector3 pos)
    {
        _ai.isStopped   = false;
        _ai.destination = pos;
    }

    private void SetSpeed(float speed)
    {
        _ai.maxSpeed = speed;
    }

    private void StopMoving()
    {
        _ai.isStopped = true;
    }

    private bool HasArrived()
    {
        return !_ai.pathPending && _ai.remainingDistance <= arrivalThreshold;
    }

    // -----------------------------------------------------------------
    //  Utilities
    // -----------------------------------------------------------------

    private void SetFOVVisible(bool visible)
    {
        if (_fov != null) _fov.enabled = visible;
    }

    private void OnDestroy()
    {
        if (_currentCover != null) _currentCover.Release(this);
    }
}
