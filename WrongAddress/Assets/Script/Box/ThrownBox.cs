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

    public void Init(float weight, Vector3 launchVelocity)
    {
        _rb.mass = weight;          
        _rb.isKinematic = false;

        _rb.velocity = launchVelocity;   
        _rb.angularVelocity = Vector3.zero;

        if (_lifeRoutine != null) StopCoroutine(_lifeRoutine);
        _lifeRoutine = StartCoroutine(LifeTimer());
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
