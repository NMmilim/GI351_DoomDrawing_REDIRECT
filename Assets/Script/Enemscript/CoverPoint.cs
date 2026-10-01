using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CoverPoint — Mark a position as a valid cover slot for ranged enemies.
///
/// SETUP
///   • Add this component to a small empty GameObject placed at the edge of a
///     wall, pillar, or prop — anywhere an enemy can duck behind.
///   • Enemies will automatically find and claim the nearest unclaimed point.
///   • Multiple enemies cannot share the same CoverPoint (unless maxOccupants > 1).
/// </summary>
public class CoverPoint : MonoBehaviour
{
    // -----------------------------------------------------------------
    //  Inspector
    // -----------------------------------------------------------------

    [Tooltip("Maximum number of enemies that can simultaneously occupy this cover point.")]
    [Range(1, 4)]
    public int maxOccupants = 1;

    [Tooltip("Visual radius shown in the Scene view.")]
    public float gizmoRadius = 0.25f;

    // -----------------------------------------------------------------
    //  Static Registry
    // -----------------------------------------------------------------

    private static readonly List<CoverPoint> _allPoints = new List<CoverPoint>();

    // -----------------------------------------------------------------
    //  Instance State
    // -----------------------------------------------------------------

    private readonly List<Enemy_Range> _occupants = new List<Enemy_Range>();

    /// <summary>True when this point is fully occupied.</summary>
    public bool IsFull => _occupants.Count >= maxOccupants;

    // -----------------------------------------------------------------
    //  Lifecycle
    // -----------------------------------------------------------------

    private void OnEnable()  => _allPoints.Add(this);
    private void OnDisable() => _allPoints.Remove(this);

    // -----------------------------------------------------------------
    //  Claim / Release
    // -----------------------------------------------------------------

    public void Claim(Enemy_Range enemy)
    {
        if (!_occupants.Contains(enemy))
            _occupants.Add(enemy);
    }

    public void Release(Enemy_Range enemy)
    {
        _occupants.Remove(enemy);
    }

    // -----------------------------------------------------------------
    //  Static Search
    // -----------------------------------------------------------------

    /// <summary>
    /// Returns the closest available CoverPoint to fromPos that is not
    /// already claimed by requester. Returns null if none are available.
    /// </summary>
    public static CoverPoint FindBestCover(Vector3 fromPos, Enemy_Range requester)
    {
        CoverPoint best    = null;
        float      bestDst = float.MaxValue;

        foreach (CoverPoint cp in _allPoints)
        {
            if (cp == null || cp.IsFull) continue;
            if (cp._occupants.Contains(requester)) continue;

            float dst = Vector3.Distance(fromPos, cp.transform.position);
            if (dst < bestDst)
            {
                bestDst = dst;
                best    = cp;
            }
        }

        return best;
    }

    // -----------------------------------------------------------------
    //  Scene Gizmos
    // -----------------------------------------------------------------

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = IsFull
            ? new Color(1f, 0.3f, 0.2f, 0.8f)
            : new Color(0.2f, 1f, 0.5f, 0.8f);
        Gizmos.DrawSphere(transform.position, gizmoRadius);
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);
    }
#endif
}
