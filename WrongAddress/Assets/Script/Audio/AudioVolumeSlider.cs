using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Binds a UI Slider to an AudioChannel in AudioManager at runtime.
/// Avoids broken serialized event refs when scenes reload (singleton survives).
/// </summary>
[RequireComponent(typeof(Slider))]
public class AudioVolumeSlider : MonoBehaviour
{
    [SerializeField] private AudioChannel _channel = AudioChannel.Master;
    [Tooltip("Re-sync slider value from AudioManager each time object enables.")]
    [SerializeField] private bool _syncOnEnable = true;

    private Slider _slider;

    private void Awake()
    {
        _slider = GetComponent<Slider>();
        _slider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void Start()
    {
        SyncFromManager();
    }

    private void OnEnable()
    {
        if (_syncOnEnable)
            SyncFromManager();
    }

    private void OnDestroy()
    {
        if (_slider != null)
            _slider.onValueChanged.RemoveListener(OnSliderValueChanged);
    }

    private void OnSliderValueChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetVolume(_channel, value);
    }

    /// <summary>
    /// Sets the slider UI to match the current AudioManager setting (no callback).
    /// </summary>
    public void SyncFromManager()
    {
        if (AudioManager.Instance == null || _slider == null)
            return;

        float v = AudioManager.Instance.GetVolume01(_channel);
        _slider.SetValueWithoutNotify(v);
    }
}
