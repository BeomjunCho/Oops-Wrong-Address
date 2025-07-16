using System.Collections;
using UnityEngine;

/// <summary>
/// Global wind source. Changes direction and strength every _interval seconds.
/// </summary>
[DefaultExecutionOrder(-190)]   // WindManager right after
public class WindManager : MonoBehaviour
{
    /* ------------------------------------------------------------------ */
    /*  Singleton                                                         */
    /* ------------------------------------------------------------------ */
    public static WindManager Instance { get; private set; }

    /* ------------------------------------------------------------------ */
    /*  Inspector                                                         */
    /* ------------------------------------------------------------------ */
    [Header("Wind Settings")]
    [SerializeField, Tooltip("Seconds between wind changes")]
    private float _interval = 15f;

    [SerializeField, Tooltip("Minimum wind speed (m/s)")]
    private float _minSpeed = 1f;

    [SerializeField, Tooltip("Maximum wind speed (m/s)")]
    private float _maxSpeed = 20f;

    /* ------------------------------------------------------------------ */
    /*  Runtime data                                                      */
    /* ------------------------------------------------------------------ */
    private Vector3 _windVelocity = Vector3.zero;

    private AudioClip _windClip;

    /// <summary>Current wind velocity in m/s (XZ plane).</summary>
    public Vector3 currentWind => _windVelocity;

    /* ------------------------------------------------------------------ */
    /*  Event                                                             */
    /* ------------------------------------------------------------------ */

    public event System.Action<Vector3> OnWindChanged;

    /* ------------------------------------------------------------------ */
    /*  Unity lifecycle                                                   */
    /* ------------------------------------------------------------------ */
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        _windClip = AudioManager.Instance.GetSfx("Wind");
        StartCoroutine(WindLoop());
    }

    /* ------------------------------------------------------------------ */
    /*  Coroutines                                                        */
    /* ------------------------------------------------------------------ */
    private IEnumerator WindLoop()
    {
        while (true)
        {
            PickRandomWind();
            Debug.Log($"[Wind] dir={_windVelocity.normalized}  speed={_windVelocity.magnitude:F1} m/s");
            yield return new WaitForSeconds(_interval);
        }
    }

    /* ------------------------------------------------------------------ */
    /*  Helpers                                                           */
    /* ------------------------------------------------------------------ */
    private void PickRandomWind()
    {
        // Four cardinal directions on XZ plane
        Vector3[] dirs = {
            Vector3.forward,  // +Z  (North)
            Vector3.back,     // -Z  (South)
            Vector3.left,     // -X  (West)
            Vector3.right     // +X  (East)
        };

        Vector3 dir = dirs[Random.Range(0, dirs.Length)];
        float spd = Random.Range(_minSpeed, _maxSpeed);

        _windVelocity = dir * spd;

        OnWindChanged?.Invoke(_windVelocity);
    }
    public void ResetWind()
    {
        StopAllCoroutines();
        _windVelocity = Vector3.zero;
        StartCoroutine(WindLoop());     // restart 15-sec wind cycle
    }
}
