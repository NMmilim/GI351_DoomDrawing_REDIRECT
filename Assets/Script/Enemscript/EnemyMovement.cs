using UnityEngine;
using Pathfinding;

/// <summary>
/// Encapsulates 2D top-down pathfinding movement using A* Pathfinding Project (AIPath + Seeker).
/// Provides arrival queries and stuck-detection recovery helpers.
/// </summary>
[RequireComponent(typeof(AIPath))]
[RequireComponent(typeof(Seeker))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Navigation Thresholds")]
    [Tooltip("Distance threshold to consider 'arrived' at the current target destination.")]
    public float arrivalThreshold = 0.35f;

    [Header("Stuck Recovery")]
    [Tooltip("If the enemy does not travel stuckMoveThreshold units within stuckTimeout seconds, it is stuck.")]
    public float stuckTimeout = 10f;
    public float stuckMoveThreshold = 0.15f;

    private AIPath   _ai;
    private float    _stuckTimer;
    private Vector3  _stuckLastPos;

    public bool IsStopped => _ai != null && _ai.isStopped;
    public Vector3 Velocity => _ai != null ? _ai.velocity : Vector3.zero;

    private void Awake()
    {
        _ai = GetComponent<AIPath>();
        _ai.enableRotation = false; // Rotation is controlled by the enemy archetype / abilities
        _ai.gravity        = Vector3.zero;
        _ai.orientation    = OrientationMode.YAxisForward;
    }

    private void Start()
    {
        ResetStuckTimer();
    }

    public void SetDestination(Vector3 targetPos)
    {
        if (_ai == null) return;
        _ai.isStopped   = false;
        _ai.destination = targetPos;
    }

    public void SetSpeed(float speed)
    {
        if (_ai != null) _ai.maxSpeed = speed;
    }

    public void StopMoving()
    {
        if (_ai != null) _ai.isStopped = true;
    }

    public bool HasArrived()
    {
        return _ai != null && !_ai.pathPending && _ai.remainingDistance <= arrivalThreshold;
    }

    public void ResetStuckTimer()
    {
        _stuckTimer   = 0f;
        _stuckLastPos = transform.position;
    }

    /// <summary>
    /// Checks whether the unit failed to make progress over the stuckTimeout window.
    /// Returns true if stuck.
    /// </summary>
    public bool CheckStuck()
    {
        _stuckTimer += Time.deltaTime;
        if (_stuckTimer < stuckTimeout) return false;

        float moved = Vector3.Distance(transform.position, _stuckLastPos);
        ResetStuckTimer();
        return moved < stuckMoveThreshold;
    }
}

