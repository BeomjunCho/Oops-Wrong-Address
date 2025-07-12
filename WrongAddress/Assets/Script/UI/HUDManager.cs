using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Heads-Up Display: wind + current / next box info.
/// Wind section updates instantly via WindManager.OnWindChanged event.
/// Box section polls PlayerController once per frame (cheap) and refreshes only
/// when the current or next box actually changes.
/// </summary>
public class HUDManager : MonoBehaviour
{
    /* ------------------------------------------------------------------ */
    /*  Wind UI                                                           */
    /* ------------------------------------------------------------------ */
    [Header("Wind Icons")]
    [SerializeField] private Image _windDirImage;
    [SerializeField] private Sprite _northSprite;   // +Z
    [SerializeField] private Sprite _eastSprite;    // +X
    [SerializeField] private Sprite _southSprite;   // -Z
    [SerializeField] private Sprite _westSprite;    // -X

    [SerializeField] private TMP_Text _speedText;

    /* ------------------------------------------------------------------ */
    /*  📦 Box HUD                                                         */
    /* ------------------------------------------------------------------ */
    [Header("Box Icons & Text")]
    [SerializeField] private PlayerController _player;

    [SerializeField] private Image _curBoxImage;
    [SerializeField] private TMP_Text _curBoxWeight;

    [SerializeField] private Image _nextBoxImage;
    [SerializeField] private TMP_Text _nextBoxWeight;

    [System.Serializable]
    private struct WeightIconPair
    {
        public float weight;   // e.g. 1,3,5,10,15,20
        public Sprite icon;     // matching sprite
    }

    [Tooltip("Weight-to-icon lookup table")]
    [SerializeField] private WeightIconPair[] _iconTable;

    /*  Cached state to avoid unnecessary UI updates                       */
    private BoxSO _cachedCurBox;
    private BoxSO _cachedNextBox;

    /* ------------------------------------------------------------------ */
    /*  Unity lifecycle                                                   */
    /* ------------------------------------------------------------------ */
    private void Start()
    {
        ApplyWind(WindManager.Instance?.currentWind ?? Vector3.zero);
        RefreshBoxHUD();

        if (WindManager.Instance != null)
            WindManager.Instance.OnWindChanged += ApplyWind;
    }

    private void OnDestroy()
    {
        if (WindManager.Instance != null)
            WindManager.Instance.OnWindChanged -= ApplyWind;
    }

    private void Update()
    {
        if (_player == null) return;

        if (_player.currentBox != _cachedCurBox || _player.nextBox != _cachedNextBox)
            RefreshBoxHUD();
    }

    /* ------------------------------------------------------------------ */
    /*  Wind helpers                                                      */
    /* ------------------------------------------------------------------ */
    private void ApplyWind(Vector3 wind)
    {
        Vector3 n = wind.sqrMagnitude < 0.0001f ? Vector3.zero : wind.normalized;

        _windDirImage.sprite =
            Vector3.Dot(n, Vector3.forward) > 0.707f ? _northSprite :
            Vector3.Dot(n, Vector3.back) > 0.707f ? _southSprite :
            Vector3.Dot(n, Vector3.right) > 0.707f ? _eastSprite :
                                                       _westSprite;

        _speedText.text = $"{wind.magnitude:F0} m/s";
    }

    /* ------------------------------------------------------------------ */
    /*  Box helpers                                                       */
    /* ------------------------------------------------------------------ */
    private void RefreshBoxHUD()
    {
        _cachedCurBox = _player.currentBox;
        _cachedNextBox = _player.nextBox;

        SetBoxUI(_cachedCurBox, _curBoxImage, _curBoxWeight);
        SetBoxUI(_cachedNextBox, _nextBoxImage, _nextBoxWeight);
    }

    private void SetBoxUI(BoxSO box, Image img, TMP_Text txt)
    {
        if (box != null)
        {
            img.sprite = FindIconFor(box.weight);
            img.enabled = true;
            txt.text = $"{box.weight:F0} kg";
        }
        else
        {
            img.enabled = false;
            txt.text = string.Empty;
        }
    }

    private Sprite FindIconFor(float weight)
    {
        foreach (var pair in _iconTable)
            if (Mathf.Approximately(pair.weight, weight))
                return pair.icon;
        return null;
    }
}
