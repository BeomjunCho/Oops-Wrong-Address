using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows pure charge gauges (0-1) for jump/throw.
/// Gauge fills fully when the key is held long enough,
/// regardless of the current box weight.
/// </summary>
public class JumpThrowGaugeUI : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private PlayerController _player;

    [Header("Jump Gauge")]
    [SerializeField] private GameObject _jumpGroup;
    [SerializeField] private Image _jumpFill;

    [Header("Throw Gauge")]
    [SerializeField] private GameObject _throwGroup;
    [SerializeField] private Image _throwFill;


    private void Update()
    {
        if (_player == null) return;

        /* -------------------- visibility -------------------- */
        _jumpGroup.SetActive(_player.isChargingJump);
        _throwGroup.SetActive(_player.isChargingThrow);

        /* -------------------- fill (pure charge) ------------ */
        _jumpFill.fillAmount = _player.jumpCharge;   // 0-1
        _throwFill.fillAmount = _player.throwCharge;  // 0-1
    }
}
