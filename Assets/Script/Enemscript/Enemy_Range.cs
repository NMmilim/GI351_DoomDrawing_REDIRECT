using System.Collections;
using UnityEngine;
using UnityEngine.AI;   // NavMesh

/// <summary>
/// Ranged enemy AI — cover-seek, peek-and-fire, roam, and alert-broadcast behaviour.
///
/// ══════════════════════════════════════════════════════════════════
///  WHY NavMeshAgent?
///   Direct Rigidbody movement (linearVelocity) worked for open floors, but it
///   ignores the navigation mesh, so enemies walked straight through walls or
///   got permanently stuck in corners — exactly the "facing through walls" bug.
///   NavMeshAgent path-finds around obstacles automatically, respects wall
///   geometry, handles moving between rooms, and is the Unity-recommended
///   solution for character locomotion.
///
///   For 2D top-down games, the NavMesh still works — you bake it from a
///   3D perspective (XZ plane) with the scene geometry included, or use the
///   NavMeshSurface component from the AI Navigation package to bake at runtime.
///   The agent moves in XZ; the transform is then locked to Z=0 (or Y=0)
///   to stay in the 2D plane.  See SETUP REQUIREMENTS below.
/// ══════════════════════════════════════════════════════════════════
///
/// DETECTION MODEL
///   Enemies have NO knowledge of the player's position by default.
///   "Detection" requires an unobstructed FOV raycast (EnemyFOV / HasClearShot).
///   Once the player is detected, the detecting enemy broadcasts the last-known
///   position to all nearby allies.
///
/// STATE MACHINE
///   SeekCover   - Path to the nearest unclaimed CoverPoint on spawn / after alert.
///   InCover     - Wait behind cover; FOV hidden. After timer -> Peeking.
///   Peeking     - Stepped out from cover. Sub-phases:
///                  1. Walk to peek spot.
///                  2. Noticing: brief delay before firing when player is spotted.
///                  3. Firing burst.
///                  When burst done -> Returning.
///   Returning   - Walk back to cover position -> InCover.
///   Roaming     - Slow walk, sweeping the map for the player.
///                  Triggered after lostSightRoamDelay s of losing sight, OR
///                  within spawnRoamDelay s of spawn if player is never detected.
///
/// SETUP REQUIREMENTS
///   * Script on the enemy root GameObject.
///   * NavMeshAgent component on the same GameObject (RequireComponent enforces this).
///     - For 2D: import the "AI Navigation" package (com.unity.ai.navigation),
///       add a NavMeshSurface to your scene, set Agent Type to "Humanoid" or a
///       custom agent, then click Bake. For a top-down 2D scene, use a very
///       small agent height (e.g. 0.1 m) so the flat geometry is included.
///     - Agent speed, stopping distance and angular speed are controlled at runtime
///       by this script — you do NOT need to set them on the component.
///   * Child GameObject with EnemyFOV (auto-found, optional).
///   * Assign bulletPre, firepos, bloodSplatPrefab in the Inspector.
///   * Player tag must be "Player".
///   * Place CoverPoint components at wall/prop edges in the scene.
///   * obstacleMask should include your Wall/Obstruction layer.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
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

    [Tooltip("Blood splat prefab (Bloodsplat.prefab).")]
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
    [Tooltip("NavMesh speed when running to cover or closing in.")]
    public float moveSpeed = 3f;

    [Tooltip("Separate slower NavMesh speed used during the Roaming state. " +
             "Simulates a cautious walking pace. Adjustable here in the Inspector.")]
    public float roamSpeed = 1.2f;

    [Tooltip("Distance threshold to consider 'arrived' at a position.")]
    public float arrivalThreshold = 0.35f;

    // -----------------------------------------------------------------
    //  Inspector - Peek Behaviour
    // -----------------------------------------------------------------

    [Header("Peek Behaviour")]
    [Tooltip("How far the enemy steps out from the cover position toward the player.")]
    public float peekOffset = 1.2f;

    [Tooltip("Number of shots to fire per peek before retreating.")]
    [Range(1, 8)]
    public int burstCount = 3;

    [Tooltip("Seconds between each shot in the burst.")]
    public float firerate = 0.6f;

    [Tooltip("How long the enemy waits at the peek position for a clear shot " +
             "before retreating.")]
    public float peekTimeout = 2.5f;

    [Tooltip("Minimum seconds to wait in cover before peeking again.")]
    public float coverWaitMin = 1.0f;

    [Tooltip("Maximum seconds to wait in cover before peeking again.")]
    public float coverWaitMax = 3.0f;

    [Tooltip("Degrees per second the body rotates to face the aim direction.")]
    public float aimRotationSpeed = 240f;

    // -----------------------------------------------------------------
    //  Inspector - Detection / Noticing
    // -----------------------------------------------------------------

    [Header("Detection / Noticing")]
    [Tooltip("Seconds of clear line-of-sight required before the enemy actually " +
             "reacts (the noticing delay). During this window it does NOT shoot.")]
    public float noticingTime = 0.6f;

    [Tooltip("Seconds after losing sight of the player before the enemy " +
             "enters the Roaming state.")]
    public float lostSightRoamDelay = 20f;

    [Tooltip("If the player is not detected within this many seconds of spawn, " +
             "the enemy enters the Roaming state immediately.")]
    public float spawnRoamDelay = 10f;

    [Tooltip("Radius (world units) within which this enemy broadcasts " +
             "a detected player location to allies.")]
    public float alertBroadcastRadius = 15f;

    // -----------------------------------------------------------------
    //  Inspector - Roaming
    // -----------------------------------------------------------------

    [Header("Roaming")]
    [Tooltip("How long the enemy pauses at each roam waypoint before picking a new one.")]
    public float roamWaypointPause = 1.5f;

    [Tooltip("Maximum distance from the enemy's position when picking a random " +
             "roam waypoint on the NavMesh.")]
    public float roamWaypointRadius = 8f;

    // -----------------------------------------------------------------
    //  Inspector - Combat
    // -----------------------------------------------------------------

    [Header("Combat")]
    [Tooltip("Layer(s) that block bullets — used for the clear-shot raycast.")]
    public LayerMask obstacleMask;

    // -----------------------------------------------------------------
    //  Inspector - Blood Splatter
    // -----------------------------------------------------------------

    [Header("Blood Splatter")]
    [Range(1, 8)]
    public int splatsPerHit = 3;
    public float splatRadius = 0.6f;

    // -----------------------------------------------------------------
    //  Private - State Machine
    // -----------------------------------------------------------------

    private enum State
    {
        SeekCover,   // Pathing to a CoverPoint
        InCover,     // Waiting behind cover
        Peeking,     // Stepped out — noticing phase then firing burst
        Returning,   // Walking back to cover after a peek
        Roaming      // Slow cautious sweep when player is lost / not yet seen
    }

    private State _state = State.SeekCover;

    // -----------------------------------------------------------------
    //  Private - References & Core
    // -----------------------------------------------------------------

    private int          _hp;
    private NavMeshAgent _agent;
    private EnemyFOV     _fov;
    private Transform    _player;

    // -----------------------------------------------------------------
    //  Private - Timers & Counters
    // -----------------------------------------------------------------

    private float _stateTimer;         // general countdown for current state
    private float _fireCooldown;       // time between individual shots
    private float _seekCoverTimer;     // prevents getting stuck seeking cover
    private float _noticingTimer;      // cumulative clear-LOS time while at peek spot
    private float _lostSightTimer;     // counts up after losing clear LOS
    private float _spawnTimer;         // counts up from spawn; triggers roam if no detection
    private float _roamWaypointTimer;  // pause timer at each roam waypoint

    // -----------------------------------------------------------------
    //  Private - Cover & Peek
    // -----------------------------------------------------------------

    private CoverPoint _currentCover;
    private Vector3    _coverPosition;   // world pos of claimed cover slot
    private Vector3    _peekPosition;    // world pos enemy steps out to
    private bool       _atPeekPosition;  // true once the enemy has walked to peekPosition
    private int        _shotsThisPeek;   // shots fired in the current peek burst
    private bool       _noticed;         // true once noticing delay completed this peek

    // -----------------------------------------------------------------
    //  Private - Player Memory
    // -----------------------------------------------------------------

    private bool    _playerEverSeen;  // true once any clear LOS was confirmed (or ally alert received)
    private Vector3 _playerLastKnown; // last world position with confirmed LOS
    private bool    _haveLastKnown;   // prevents using Vector3.zero as a valid last-known

    // -----------------------------------------------------------------
    //  Private - Spawn Roam gate
    // -----------------------------------------------------------------

    private bool _spawnRoamTriggered; // only fire the spawn->roam transition once

    // -----------------------------------------------------------------
    //  Static - Team Alert
    //  When any enemy fires / detects, all teammates in InCover skip their
    //  wait timer and peek immediately.
    // -----------------------------------------------------------------

    private static float _teamAlertTimer = 0f;

    private static void BroadcastTeamAlert()
    {
        _teamAlertTimer = 6f;
    }

    private static bool TeamIsAlerted => _teamAlertTimer > 0f;

    // -----------------------------------------------------------------
    //  Unity Messages
    // -----------------------------------------------------------------

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _fov   = GetComponentInChildren<EnemyFOV>();

        // 2D NavMesh setup — disable the agent's built-in rotation and Y-axis
        // correction so we can handle rotation ourselves and stay on the XY plane.
        _agent.updateRotation = false;
        _agent.updateUpAxis   = false;
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

        _spawnTimer         = 0f;
        _spawnRoamTriggered = false;
        _lostSightTimer     = 0f;

        // Stagger so multiple enemies don't all act simultaneously
        Invoke(nameof(EnterSeekCover), Random.Range(0f, 0.5f));
    }

    private void Update()
    {
        if (_player == null) return;

        // Tick global team-alert countdown
        if (_teamAlertTimer > 0f)
            _teamAlertTimer -= Time.deltaTime;
        if (_fireCooldown > 0f)
            _fireCooldown -= Time.deltaTime;

        // ---- Spawn-roam gate ---------------------------------------
        // If no detection within spawnRoamDelay seconds of spawning, go roam.
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
        // After detection, count up when we have no clear shot.
        // After lostSightRoamDelay, switch to roaming.
        if (_playerEverSeen && _state != State.Roaming)
        {
            if (HasClearShot())
            {
                _lostSightTimer = 0f;
            }
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
    //  Find nearest unclaimed CoverPoint and path to it via NavMesh.
    // -----------------------------------------------------------------

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
            return;
        }

        SetAgentSpeed(moveSpeed);
        SetAgentDestination(_coverPosition);
    }

    private void TickSeekCover()
    {
        _seekCoverTimer -= Time.deltaTime;
        if (_seekCoverTimer <= 0f)
        {
            StopAgent();
            _coverPosition = transform.position;
            if (_currentCover != null) { _currentCover.Release(this); _currentCover = null; }
            EnterInCover();
            return;
        }

        if (HasAgentArrived())
        {
            StopAgent();
            EnterInCover();
        }
    }

    // -----------------------------------------------------------------
    //  State: InCover
    //  Wait behind cover. After timer (or team alert) -> peek.
    // -----------------------------------------------------------------

    private void EnterInCover()
    {
        _state      = State.InCover;
        _stateTimer = Random.Range(coverWaitMin, coverWaitMax);

        StopAgent();
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
    //  Phase 1 — Walk to peek position.
    //  Phase 2 — Noticing delay (see player continuously for noticingTime).
    //  Phase 3 — Fire burst.
    //  On timeout -> Returning.
    // -----------------------------------------------------------------

    private void EnterPeeking()
    {
        _state          = State.Peeking;
        _stateTimer     = peekTimeout;
        _shotsThisPeek  = 0;
        _atPeekPosition = false;
        _noticingTimer  = 0f;
        _noticed        = false;

        // Step out from cover toward last-known / live player position
        Vector3 target   = _haveLastKnown ? _playerLastKnown : _player.position;
        Vector3 dir      = (target - _coverPosition).normalized;
        _peekPosition    = _coverPosition + dir * peekOffset;

        SetAgentSpeed(moveSpeed);
        SetAgentDestination(_peekPosition);
        SetFOVVisible(true);
    }

    private void TickPeeking()
    {
        _stateTimer -= Time.deltaTime;

        // ---- Phase 1: Walk to peek position -----------------------
        if (!_atPeekPosition)
        {
            if (!HasAgentArrived()) return; // still walking
            StopAgent();
            _atPeekPosition = true;
        }

        // ---- Phase 2: Noticing delay before first shot ------------
        if (!_noticed)
        {
            if (HasClearShot())
            {
                _noticingTimer += Time.deltaTime;
                if (_noticingTimer >= noticingTime)
                {
                    _noticed = true;
                    // Commit detection — record last known and alert allies
                    _playerLastKnown = _player.position;
                    _playerEverSeen  = true;
                    _haveLastKnown   = true;
                    AlertNearbyAllies(_playerLastKnown);
                    BroadcastTeamAlert();
                    _spawnRoamTriggered = true;
                    _lostSightTimer     = 0f;
                    // Fire first shot immediately
                    TakeShot();
                }
                // Else: still in noticing window — wait, do not fire
            }
            else
            {
                _noticingTimer = 0f; // lost LOS during noticing — reset
            }
        }
        else
        {
            // ---- Phase 3: Fire burst -------------------------------
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

        // ---- Peek timeout -----------------------------------------
        if (_stateTimer <= 0f)
        {
            if (_haveLastKnown)
                ShootAt(_playerLastKnown); // speculative suppressive shot
            EnterReturning();
        }
    }

    // -----------------------------------------------------------------
    //  State: Returning
    //  Walk back to cover. FOV hidden.
    // -----------------------------------------------------------------

    private void EnterReturning()
    {
        _state = State.Returning;
        StopAgent();
        SetFOVVisible(false);
        SetAgentSpeed(moveSpeed);
        SetAgentDestination(_coverPosition);
    }

    private void TickReturning()
    {
        if (HasAgentArrived())
        {
            StopAgent();
            EnterInCover();
        }
    }

    // -----------------------------------------------------------------
    //  State: Roaming
    //  Slow cautious walk — sweeps the map for the player.
    //  Triggered after lostSightRoamDelay s without sight,
    //  OR spawnRoamDelay s after spawn with no detection.
    //  Spotting the player -> SeekCover (tactical response).
    // -----------------------------------------------------------------

    private void EnterRoaming()
    {
        _state             = State.Roaming;
        _roamWaypointTimer = 0f;
        _lostSightTimer    = 0f;

        StopAgent();
        SetFOVVisible(true);
        SetAgentSpeed(roamSpeed);

        PickRoamWaypoint();
    }

    private void TickRoaming()
    {
        // If we spot the player while roaming -> seek cover tactically
        if (HasClearShot())
        {
            _playerLastKnown    = _player.position;
            _playerEverSeen     = true;
            _haveLastKnown      = true;
            _spawnRoamTriggered = true;
            _lostSightTimer     = 0f;

            AlertNearbyAllies(_playerLastKnown);
            BroadcastTeamAlert();

            StopAgent();
            EnterSeekCover();
            return;
        }

        if (HasAgentArrived())
        {
            StopAgent();
            _roamWaypointTimer += Time.deltaTime;
            if (_roamWaypointTimer >= roamWaypointPause)
            {
                _roamWaypointTimer = 0f;
                PickRoamWaypoint();
            }
        }
    }

    /// <summary>Pick a random reachable NavMesh point and set it as destination.</summary>
    private void PickRoamWaypoint()
    {
        Vector3 randomDir = Random.insideUnitSphere * roamWaypointRadius;
        randomDir.z = 0f;
        randomDir += transform.position;

        if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, roamWaypointRadius, NavMesh.AllAreas))
        {
            SetAgentSpeed(roamSpeed);
            SetAgentDestination(hit.position);
        }
        // No valid point found — try again on next pause cycle
    }

    // -----------------------------------------------------------------
    //  Alert Broadcast
    // -----------------------------------------------------------------

    /// <summary>
    /// Notifies all Enemy_Range instances within alertBroadcastRadius of the
    /// player's last-known position. Allies update their own last-known and
    /// react immediately if in InCover or Roaming.
    /// </summary>
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

    /// <summary>
    /// Called by a teammate who has detected the player.
    /// Updates last-known position and causes idle enemies to react immediately.
    /// </summary>
    public void ReceiveAlert(Vector3 knownPlayerPos)
    {
        _playerLastKnown    = knownPlayerPos;
        _haveLastKnown      = true;
        _playerEverSeen     = true;
        _spawnRoamTriggered = true;
        _lostSightTimer     = 0f;

        if (_state == State.InCover)
            EnterPeeking();
        else if (_state == State.Roaming)
        {
            StopAgent();
            EnterSeekCover();
        }
    }

    // -----------------------------------------------------------------
    //  Combat
    // -----------------------------------------------------------------

    /// <summary>
    /// Controls body rotation based on awareness.
    ///   Unaware (no last-known): face NavMesh movement direction.
    ///   Aware: track last-known or live player position.
    /// </summary>
    private void UpdateBodyRotation()
    {
        float targetAngle;

        if (_haveLastKnown || _state == State.Peeking)
        {
            Vector3 lookTarget = (_state == State.Peeking && _player != null)
                                 ? _player.position
                                 : _playerLastKnown;
            Vector2 dir  = ((Vector2)lookTarget - (Vector2)transform.position).normalized;
            targetAngle  = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        }
        else
        {
            Vector3 vel = _agent.velocity;
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

    /// <summary>Plain obstacle raycast — no cone restriction.</summary>
    private bool HasClearShot()
    {
        if (_player == null || firepos == null) return false;
        Vector2 origin = firepos.position;
        Vector2 dir    = ((Vector2)_player.position - origin).normalized;
        float   dist   = Vector2.Distance(origin, _player.position);
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, dist, obstacleMask);
        return hit.collider == null;
    }

    /// <summary>Fire one bullet, tick cooldown and shot counter.</summary>
    private void TakeShot()
    {
        ShootAt(_player.position);
        _fireCooldown = firerate;
        _shotsThisPeek++;
    }

    /// <summary>Fire one bullet toward an arbitrary world position (e.g. last-known).</summary>
    private void ShootAt(Vector3 targetPos)
    {
        if (bulletPre == null || firepos == null) return;
        Vector2 shootDir = ((Vector2)targetPos - (Vector2)firepos.position).normalized;
        float   angle    = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg;
        Instantiate(bulletPre, firepos.position, Quaternion.Euler(0f, 0f, angle));
    }

    // -----------------------------------------------------------------
    //  Damage / Death
    // -----------------------------------------------------------------

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

    // -----------------------------------------------------------------
    //  NavMesh Helpers
    // -----------------------------------------------------------------

    private void SetAgentDestination(Vector3 pos)
    {
        if (_agent == null || !_agent.isOnNavMesh) return;
        _agent.isStopped   = false;
        _agent.destination = pos;
    }

    private void SetAgentSpeed(float speed)
    {
        if (_agent != null) _agent.speed = speed;
    }

    private void StopAgent()
    {
        if (_agent == null || !_agent.isOnNavMesh) return;
        _agent.isStopped = true;
        _agent.velocity  = Vector3.zero;
    }

    /// <summary>
    /// Returns true when the NavMeshAgent has reached its destination
    /// (path is not pending and remaining distance is within arrivalThreshold).
    /// </summary>
    private bool HasAgentArrived()
    {
        if (_agent == null || !_agent.isOnNavMesh) return true;
        if (_agent.pathPending) return false;
        return _agent.remainingDistance <= arrivalThreshold;
    }

    // -----------------------------------------------------------------
    //  Utilities
    // -----------------------------------------------------------------

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
