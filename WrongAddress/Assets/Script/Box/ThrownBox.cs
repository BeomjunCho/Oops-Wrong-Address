using System.Collections;
using UnityEngine;

/// <summary>
/// Physics, timed life, and pooling for a thrown box.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ThrownBox : MonoBehaviour
{
    [SerializeField] private float _lifeTime = 15f;

    private Rigidbody _rb;
    private Coroutine _lifeRoutine;

    private void Awake() => _rb = GetComponent<Rigidbody>();

    private void OnDisable()
    {
        if (_lifeRoutine != null) { StopCoroutine(_lifeRoutine); _lifeRoutine = null; }
    }

    public void Init(float weight,
                 Vector3 launchVelocity,
                 PlayerController owner)
    {
        _rb.mass = weight;
        _rb.isKinematic = false;

        IgnorePlayerCollision(owner, 0.3f);

        _rb.velocity = launchVelocity;
        _rb.angularVelocity = Vector3.zero;

        /* NEW: wind starts only after velocity is set */
        GetComponent<BoxWindReceiver>()?.ActivateWind();

        if (_lifeRoutine != null) StopCoroutine(_lifeRoutine);
        _lifeRoutine = StartCoroutine(LifeTimer());
    }

    /* ------------------------------------------------------------------ */
    /*  Utilities                                                          */
    /* ------------------------------------------------------------------ */
    private void IgnorePlayerCollision(PlayerController pc, float duration)
    {
        var playerCC = pc.GetComponent<CharacterController>();
        if (playerCC == null) return;

        var boxCol = GetComponent<Collider>();
        if (boxCol == null) return;

        Physics.IgnoreCollision(boxCol, playerCC, true);
        StartCoroutine(RestoreCollision(boxCol, playerCC, duration));
    }

    private IEnumerator RestoreCollision(Collider boxCol,
                                          Collider playerCol,
                                          float delay)
    {
        yield return new WaitForSeconds(delay);
        if (boxCol != null && playerCol != null)
            Physics.IgnoreCollision(boxCol, playerCol, false);
    }

    private IEnumerator LifeTimer()
    {
        yield return new WaitForSeconds(_lifeTime);
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        _rb.velocity = _rb.angularVelocity = Vector3.zero;
        BoxPool.Instance.Return(gameObject);
    }
}
