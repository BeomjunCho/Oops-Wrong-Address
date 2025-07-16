using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Automatically plays UI button hover and click sounds using AudioManager.
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButtonAudio : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("SFX Keys (must match AudioManager SFX entries)")]
    [Tooltip("Key for hover sound")]
    [SerializeField] private string _hoverKey = "UI_ButtonHover";
    [Tooltip("Key for click sound")]
    [SerializeField] private string _clickKey = "UI_ButtonClick";
    [Range(0f, 1f)]
    [Tooltip("Playback volume")]
    [SerializeField] private float _volume = 1f;

    private Button _button;

    /// <summary>
    /// Cache button component and bind click event.
    /// </summary>
    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnButtonClick);
    }

    /// <summary>
    /// Play hover sound when pointer enters.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        PlaySfx(_hoverKey);
    }

    /// <summary>
    /// Play click sound when pointer click.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        PlaySfx(_clickKey);
    }

    /// <summary>
    /// Helper to lookup and play the specified SFX key.
    /// </summary>
    /// <param name="key">AudioManager SFX key.</param>
    private void PlaySfx(string key)
    {
        AudioClip clip = AudioManager.Instance.GetSfx(key);
        if (clip != null)
        {
            SFX2DManager.Instance.Play2dSfx(key, clip, _volume);
        }
        else
        {
            Debug.LogWarning($"UIButtonAudio: SFX '{key}' not found in AudioManager.");
        }
    }

    /// <summary>
    /// Invoked by Button.onClick to play click sound.
    /// </summary>
    private void OnButtonClick()
    {
        PlaySfx(_clickKey);
    }
}
