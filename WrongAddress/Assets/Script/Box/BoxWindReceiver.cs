using UnityEngine;

/// <summary>
/// Applies global wind force to the box only after it is thrown.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BoxWindReceiver : MonoBehaviour
{
    [Header("Wind Response")]
    [SerializeField, Tooltip("Scale factor for wind influence (0-1)")]
    private float _windCoeff = 0.25f;

    private Rigidbody _rb;
    private bool _inAir = false;        // Wind inactive by default

    /* ------------------------------------------------------------------ */
    /*  Unity lifecycle                                                   */
    /* ------------------------------------------------------------------ */
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        // Object just pulled from pool → no wind yet
        _inAir = false;
    }

    private void FixedUpdate()
    {
        if (!_inAir) return;

        if (WindManager.Instance == null)
        {
            Debug.LogError("BoxWindReceiver: WindManager.Instance is NULL!");
            return;
        }

        Vector3 wind = WindManager.Instance.currentWind;
        Debug.Log($"BoxWindReceiver: Applying wind {wind} to {gameObject.name}");
        _rb.AddForce(wind * _windCoeff, ForceMode.Force);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // First ground contact ends wind effect
        _inAir = false;
    }

    /* ------------------------------------------------------------------ */
    /*  Public API                                                        */
    /* ------------------------------------------------------------------ */
    /// <summary>Starts applying wind from the next physics step.</summary>
    public void ActivateWind()
    {
        _inAir = true;
    }
}
