using UnityEngine;

/// <summary>
/// Represents a delivery tile.  
/// Starts box tracking when a box enters its trigger and awards points
/// when the box fully stops.
/// </summary>
[RequireComponent(typeof(Collider))]
public class HouseTile : MonoBehaviour
{
    /* ------------------------------------------------------------------ */
    /*  Inspector                                                         */
    /* ------------------------------------------------------------------ */
    [Tooltip("Point the player is trying to hit (usually tile center).")]
    [SerializeField] private Transform _deliveryPoint;

    /* ------------------------------------------------------------------ */
    /*  Trigger entry                                                     */
    /* ------------------------------------------------------------------ */
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out BoxScoringTracker tracker))
        {
            tracker.Register(this);
        }
    }

    /* ------------------------------------------------------------------ */
    /*  Called by tracker when box stops                                  */
    /* ------------------------------------------------------------------ */
    /// <summary>
    /// Receives callback from BoxScoringTracker when the box has settled.
    /// </summary>
    /// <param name="boxPos">Final world position of the box.</param>
    public void BoxStopped(Vector3 boxPos)
    {
        float distance = Vector3.Distance(boxPos, _deliveryPoint.position);
        float score = CalculateScore(distance);

        ScoreManager.Instance.AddScore(score, boxPos);
    }

    /// <summary>
    /// 100 points within 5 m, minus 10 per additional 5 m segment, not below 0.
    /// </summary>
    private float CalculateScore(float distance)
    {
        if (distance <= 3f) return 100f;

        int segments = Mathf.FloorToInt(distance / 1f);
        return Mathf.Max(0f, 100f - segments * 5f);
    }
}
