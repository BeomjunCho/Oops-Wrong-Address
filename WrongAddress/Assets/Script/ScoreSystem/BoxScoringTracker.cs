using UnityEngine;

/// <summary>
/// Attached to every thrown box.  
/// After registration by a HouseTile, watches rigidbody speed until the
/// box fully stops, then informs the tile exactly once.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BoxScoringTracker : MonoBehaviour
{
    /* ------------------------------------------------------------------ */
    /*  Constants                                                         */
    /* ------------------------------------------------------------------ */
    private const float _stopSpeedThresholdSq = 0.01f;  // (0.1 m/s)^2

    /* ------------------------------------------------------------------ */
    /*  State                                                             */
    /* ------------------------------------------------------------------ */
    private Rigidbody _rb;
    private HouseTile _trackedTile;
    private bool _tracking;

    /* ------------------------------------------------------------------ */
    /*  Unity lifecycle                                                   */
    /* ------------------------------------------------------------------ */
    private void Awake() => _rb = GetComponent<Rigidbody>();

    private void OnEnable()
    {
        // Reset between pool uses
        _trackedTile = null;
        _tracking = false;
    }

    private void FixedUpdate()
    {
        if (!_tracking) return;

        // Wait until velocity magnitude is <= 0.1 m/s
        if (_rb.velocity.sqrMagnitude <= _stopSpeedThresholdSq)
        {
            _trackedTile.BoxStopped(transform.position);
            _tracking = false;          // ensure single callback
        }
    }

    /* ------------------------------------------------------------------ */
    /*  API                                                               */
    /* ------------------------------------------------------------------ */
    /// <summary>
    /// Called by HouseTile when the box first enters its trigger.
    /// Only the first tile to call will succeed.
    /// </summary>
    public void Register(HouseTile tile)
    {
        if (_tracking) return;          // already assigned to a tile

        _trackedTile = tile;
        _tracking = true;
    }
}
