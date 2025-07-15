using System.Collections.Generic;
using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Runner player: movement, jump, camera yaw, and box throw.
/// Weight-balancing uses linear factors for smoother scaling.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    /* ------------------------------------------------------------------ */
    /*  Movement & Jump (base)                                            */
    /* ------------------------------------------------------------------ */
    [Header("Movement")]
    [SerializeField] private float _baseForwardSpeed = 6f;
    [SerializeField] private float _baseLateralSpeed = 6f;
    [SerializeField] private float _acceleration = 8f;

    [Header("Jump")]
    [SerializeField] private float _baseJumpForce = 8f;
    [SerializeField] private float _maxJumpChargeTime = 1f;

    /* ------------------------------------------------------------------ */
    /*  Horizontal Bounds                                                 */
    /* ------------------------------------------------------------------ */
    [Header("Bounds")]
    [Tooltip("World-space X range in which the player is allowed to stay.")]
    [SerializeField] private Vector2 _xBounds = new(-9f, 9f);

    /* ------------------------------------------------------------------ */
    /*  Weight Balancing Factors                                          */
    /* ------------------------------------------------------------------ */
    [Header("Weight Balancing")]
    [Tooltip("Speed multiplier at heaviest box (20 kg).")]
    [SerializeField] private float _minSpeedFactor = 0.6f;
    [Tooltip("Jump multiplier at heaviest box (20 kg).")]
    [SerializeField] private float _minJumpFactor = 0.8f;
    [Tooltip("Throw force multiplier at heaviest box (20 kg).")]
    [SerializeField] private float _minThrowFactor = 0.5f;

    /* ------------------------------------------------------------------ */
    /*  Physics                                                           */
    /* ------------------------------------------------------------------ */
    [Header("Physics")]
    [SerializeField] private float _gravity = -20f;

    /* ------------------------------------------------------------------ */
    /*  Camera                                                            */
    /* ------------------------------------------------------------------ */
    [Header("Camera")]
    [SerializeField] private Transform _cam;
    [SerializeField] private Transform _camTarget;
    [SerializeField] private Vector3 _camOffset = new(0f, 5f, -8f);
    [SerializeField] private float _camSmooth = 0.1f;
    [SerializeField] private float _camYawSpeed = 180f;

    /* ------------------------------------------------------------------ */
    /*  Throw                                                             */
    /* ------------------------------------------------------------------ */
    [Header("Throw")]
    [SerializeField] private BoxSO[] _boxLibrary;
    [SerializeField] private float _baseThrowForce = 25f;
    [SerializeField] private float _maxThrowChargeTime = 1.2f;
    [SerializeField] private float _throwAngleDeg = 25f;
    [SerializeField] private Transform _handAnchor;
    [SerializeField] private int _queueSize = 3;

    /* ------------------------------------------------------------------ */
    /*  Input                                                             */
    /* ------------------------------------------------------------------ */
    [Header("Input")]
    [SerializeField] private float _mouseLookDelay = 0.3f;

    /* ------------------------------------------------------------------ */
    /*  Trajectory / landing marker                                       */
    /* ------------------------------------------------------------------ */
    [Header("Trajectory")]
    [SerializeField] private LineRenderer _trajectoryLine;
    [SerializeField] private int _trajectorySteps = 40;
    [SerializeField] private float _trajectoryStepSec = 0.05f;
    [SerializeField] private Transform _landingMarker;         
    [SerializeField] private LayerMask _groundMask = 1 << 6;    // e.g. "Ground" layer

    /* ------------------------------------------------------------------ */
    /*  Internal state                                                    */
    /* ------------------------------------------------------------------ */
    private CharacterController _cc;
    private Coroutine _spawnRoutine;

    // movement
    private float _currentFwdSpeed;
    private float _verticalVel;
    private float _jumpCharge;
    private bool _chargingJump;

    // camera
    private float _yaw;
    private Vector3 _camVel;

    // throw
    private BoxSO _currentBox;
    private readonly Queue<BoxSO> _nextBoxes = new();
    private float _throwCharge;
    private bool _chargingThrow;
    private GameObject _heldBoxGO;

    // time
    private float _mouseLookEnableTime;

    /* ------------------------------------------------------------------ */
    /*  Public Property                                                   */
    /* ------------------------------------------------------------------ */

    /// <summary>True while Space is held for jump.</summary>
    public bool isChargingJump => _chargingJump;

    /// <summary>True while LMB is held for throw.</summary>
    public bool isChargingThrow => _chargingThrow;

    /// <summary>0-1 ratio of the actual jump impulse that will be applied.</summary>
    public float jumpPowerRatio => _jumpCharge * JumpFactor();

    /// <summary>0-1 ratio of the actual throw speed that will be applied.</summary>
    public float throwPowerRatio => _throwCharge * ThrowFactor();

    /* ================================================================== */
    /*  Unity lifecycle                                                   */
    /* ================================================================== */
    private void Awake()
    {
        _cc = GetComponent<CharacterController>();

        if (_cam == null && Camera.main != null) _cam = Camera.main.transform;
        if (_camTarget == null) _camTarget = transform;
        if (_handAnchor == null) _handAnchor = _cam;

        _yaw = transform.eulerAngles.y;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _mouseLookEnableTime = Time.time + _mouseLookDelay;

        _currentBox = PickRandomBox();
        for (int i = 0; i < _queueSize; ++i) _nextBoxes.Enqueue(PickRandomBox());

        SpawnHeldBox();
        UpdateCameraImmediate();
    }

    private void Update()
    {
        HandleMouseLook();
        HandleJumpInput();
        HandleThrowInput();
        Move();
        ClampHorizontalPosition();    // keep X inside bounds
        UpdateTrajectory();
    }

    private void LateUpdate() => FollowCamera();

    /* ================================================================== */
    /*  Mouse look                                                        */
    /* ================================================================== */
    private void HandleMouseLook()
    {
        if (Time.time < _mouseLookEnableTime) return;

        float mx = Input.GetAxis("Mouse X");
        _yaw += mx * _camYawSpeed * Time.deltaTime;
        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
    }

    /* ================================================================== */
    /*  Jump logic                                                        */
    /* ================================================================== */
    private void HandleJumpInput()
    {
        bool grounded = _cc.isGrounded;

        /* Small negative keeps CharacterController grounded */
        if (grounded && _verticalVel < 0f) _verticalVel = -2f;

        /* Hold Space to charge */
        if (Input.GetKey(KeyCode.Space) && grounded)
        {
            _chargingJump = true;
            _jumpCharge = Mathf.Clamp01(_jumpCharge + Time.deltaTime / _maxJumpChargeTime);
        }

        /* Release Space -> apply impulse */
        if (Input.GetKeyUp(KeyCode.Space) && _chargingJump && grounded)
        {
            // finalJump = base * charge * weightFactor
            _verticalVel = _baseJumpForce * _jumpCharge * JumpFactor();
            _jumpCharge = 0f;
            _chargingJump = false;
        }

        /* Integrate gravity */
        _verticalVel += _gravity * Time.deltaTime;
    }

    /* ================================================================== */
    /*  Throw logic                                                       */
    /* ================================================================== */
    private void HandleThrowInput()
    {
        /* Start charging on mouse down */
        if (Input.GetMouseButtonDown(0))
        {
            _chargingThrow = true;
            _throwCharge = 0f;
        }

        /* Hold to increase charge */
        if (Input.GetMouseButton(0) && _chargingThrow)
        {
            _throwCharge = Mathf.Clamp01(_throwCharge + Time.deltaTime / _maxThrowChargeTime);
        }

        /* Release -> throw */
        if (Input.GetMouseButtonUp(0) && _chargingThrow)
        {
            ThrowHeldBox();
            CycleNextBox();
            _chargingThrow = false;
            _throwCharge = 0f;
        }
    }

    /// <summary>
    /// Detaches current box, computes launch velocity, and hands it to ThrownBox.
    /// Important: ONLY upward (positive) Y velocity is added so that falling
    /// momentum does not cancel the throw arc (option A).
    /// </summary>
    private void ThrowHeldBox()
    {
        if (_heldBoxGO == null) return;

        _heldBoxGO.transform.SetParent(null);

        /* Player horizontal inertia */
        Vector3 playerHorVel = transform.forward * _currentFwdSpeed;

        /* Ignore downward velocity when falling (max with 0) */
        float upwardVel = Mathf.Max(0f, _verticalVel);

        /* Aim direction based on anchor forward tilted upward by _throwAngleDeg */
        Vector3 dir = Quaternion.AngleAxis(_throwAngleDeg, _handAnchor.right) *
                      _handAnchor.forward;
        dir.Normalize();

        /* Raw launch speed scaled by charge and weight */
        float speed = _baseThrowForce * _throwCharge * ThrowFactor();

        /* Final launch velocity */
        Vector3 launchVelocity = dir * speed + playerHorVel + Vector3.up * upwardVel;

        /* Pass to the ThrownBox */
        _heldBoxGO.GetComponent<ThrownBox>()
                  .Init(_currentBox.weight, launchVelocity, this);

        _heldBoxGO = null;

        /* Disable trajectory rendering */
        if (_trajectoryLine != null) _trajectoryLine.enabled = false;
    }


    private void CycleNextBox()
    {
        _currentBox = _nextBoxes.Dequeue();
        _nextBoxes.Enqueue(PickRandomBox());

        if (_spawnRoutine != null) StopCoroutine(_spawnRoutine);
        _spawnRoutine = StartCoroutine(SpawnNextBoxDelayed());
    }

    /// <summary>Dequeues the next box and spawns a fresh instance in hand.</summary>
    private IEnumerator SpawnNextBoxDelayed()
    {
        yield return new WaitForSeconds(1f);
        SpawnHeldBox();
        _spawnRoutine = null;
    }

    /// <summary>Instantiate or reuse a pooled box and attach to hand.</summary>
    private void SpawnHeldBox()
    {
        if (_heldBoxGO != null) BoxPool.Instance.Return(_heldBoxGO);

        _heldBoxGO = BoxPool.Instance.Rent(_currentBox.prefab);
        _heldBoxGO.transform.SetParent(_handAnchor);
        _heldBoxGO.transform.localPosition = Vector3.zero;
        _heldBoxGO.transform.localRotation = Quaternion.identity;

        /* Reset physics state in case the object returned from pool kept motion */
        var rb = _heldBoxGO.GetComponent<Rigidbody>();
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
    }

    /* ================================================================== */
    /*  Movement                                                          */
    /* ================================================================== */
    private void Move()
    {
        float speedFactor = SpeedFactor();
        float targetFwd = _baseForwardSpeed * speedFactor;

        _currentFwdSpeed = Mathf.Lerp(_currentFwdSpeed,
                                      targetFwd,
                                      _acceleration * Time.deltaTime);

        float h = Input.GetAxisRaw("Horizontal");

        Vector3 move = transform.forward * _currentFwdSpeed +
                       transform.right * h * _baseLateralSpeed * speedFactor;

        move.y = _verticalVel;
        _cc.Move(move * Time.deltaTime);
    }

    /// <summary>
    /// Clamps player's X position to the configured bounds.
    /// </summary>
    private void ClampHorizontalPosition()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, _xBounds.x, _xBounds.y);
        transform.position = pos;
    }

    /* ================================================================== */
    /*  Camera follow                                                     */
    /* ================================================================== */
    private void FollowCamera()
    {
        if (_cam == null) return;

        Quaternion rot = Quaternion.Euler(0f, _yaw, 0f);
        Vector3 desired = _camTarget.position + rot * _camOffset;

        _cam.position = Vector3.SmoothDamp(_cam.position, desired, ref _camVel, _camSmooth);
        _cam.LookAt(_camTarget);
    }

    private void UpdateCameraImmediate()
    {
        if (_cam == null) return;

        Quaternion rot = Quaternion.Euler(0f, _yaw, 0f);
        _cam.position = _camTarget.position + rot * _camOffset;
        _cam.LookAt(_camTarget);
    }

    /* ================================================================== */
    /*  Helpers                                                           */
    /* ================================================================== */

    /// <summary>Draws predicted arc while charging throw.</summary>
    private void UpdateTrajectory()
    {
        if (_trajectoryLine == null) return;

        /* ----- enable / disable marker & line depending on charging state ----- */
        bool active = _chargingThrow;
        if (_trajectoryLine.enabled != active) _trajectoryLine.enabled = active;
        if (_landingMarker != null && _landingMarker.gameObject.activeSelf != active)
            _landingMarker.gameObject.SetActive(active);

        if (!active) return;

        _trajectoryLine.positionCount = _trajectorySteps;

        /* ----------- compute the SAME launch velocity as ThrowHeldBox ---------- */
        Vector3 origin = _handAnchor.position;

        Vector3 dir = Quaternion.AngleAxis(_throwAngleDeg, _handAnchor.right) *
                      _handAnchor.forward;
        dir.Normalize();

        float speed = _baseThrowForce * _throwCharge * ThrowFactor();

        Vector3 horVel = transform.forward * _currentFwdSpeed;
        float yVel = Mathf.Max(0f, _verticalVel); // up-only

        Vector3 v0 = dir * speed + horVel + Vector3.up * yVel;
        Vector3 g = Physics.gravity;

        /* ----------- sample points and detect ground intersection ------------- */
        Vector3 prev = origin;
        float t = 0f;
        bool hitFound = false;

        for (int i = 0; i < _trajectorySteps; ++i)
        {
            Vector3 point = origin + v0 * t + 0.5f * g * t * t;
            _trajectoryLine.SetPosition(i, point);

            /* raycast from prev to point to find first ground hit */
            if (!hitFound)
            {
                Vector3 segment = point - prev;
                if (Physics.Raycast(prev, segment.normalized,
                                    out RaycastHit hit,
                                    segment.magnitude,
                                    _groundMask,
                                    QueryTriggerInteraction.Ignore))
                {
                    hitFound = true;

                // 1) write the hit point first (i is still < original count)
                _trajectoryLine.SetPosition(i, hit.point);
                    
                // 2) shrink the line so indices 0..i remain valid
                _trajectoryLine.positionCount = i + 1;
                    
                // 3) move landing marker
                if (_landingMarker != null)
                _landingMarker.position = hit.point;
                    
                // 4) break so we never touch out-of-range indices
                break;
                }
            }

            prev = point;
            t += _trajectoryStepSec;
        }

        /* If no hit was found, hide marker */
        if (!hitFound && _landingMarker != null)
            _landingMarker.gameObject.SetActive(false);
    }
    private BoxSO PickRandomBox() => _boxLibrary[Random.Range(0, _boxLibrary.Length)];

    private float WeightNorm()
    {
        return Mathf.Clamp01((_currentBox.weight - 1f) / 19f); // 1kg ¡æ0, 20kg¡æ1
    }

    private float SpeedFactor() => Mathf.Lerp(1f, _minSpeedFactor, WeightNorm());
    private float JumpFactor()
    {
        float t = WeightNorm();          // 0 (1kg) ~ 1 (20kg)
        return 1f - (1f - _minJumpFactor) * t * t;
    }
    private float ThrowFactor()
    {
        float t = WeightNorm();              // 0 (1 kg) ~ 1 (20 kg)
        t = Mathf.Sqrt(t);                   //
        return Mathf.Lerp(1f, _minThrowFactor, t);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying && _cam != null && _camTarget != null)
        {
            Quaternion rot = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            _cam.position = _camTarget.position + rot * _camOffset;
            _cam.LookAt(_camTarget);
        }
    }
#endif

    /* ================================================================== */
    /*  Public getters                                                    */
    /* ================================================================== */
    public float throwCharge => _throwCharge;
    public float jumpCharge => _jumpCharge;
    public BoxSO currentBox => _currentBox;
    public BoxSO nextBox => _nextBoxes.Count > 0 ? _nextBoxes.Peek() : null;
}
