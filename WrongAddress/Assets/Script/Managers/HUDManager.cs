using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Heads-Up Display: wind + current / next box info + 3-minute countdown timer.
/// </summary>
public class HUDManager : MonoBehaviour
{
    /* ------------------------------------------------------------------ */
    /*  Wind UI                                                           */
    /* ------------------------------------------------------------------ */
    [Header("Wind Icons")]
    [SerializeField] private Image _windDirImage;
    [SerializeField] private Sprite _northSprite;
    [SerializeField] private Sprite _eastSprite;
    [SerializeField] private Sprite _southSprite;
    [SerializeField] private Sprite _westSprite;
    [SerializeField] private TMP_Text _speedText;

    /* ------------------------------------------------------------------ */
    /*  📦 Box UI                                                          */
    /* ------------------------------------------------------------------ */
    [Header("Box Icons & Text")]
    [SerializeField] private PlayerController _player;
    [SerializeField] private Image _curBoxImage;
    [SerializeField] private TMP_Text _curBoxWeight;
    [SerializeField] private Image _nextBoxImage;
    [SerializeField] private TMP_Text _nextBoxWeight;

    [System.Serializable]
    private struct WeightIconPair { public float weight; public Sprite icon; }
    [SerializeField] private WeightIconPair[] _iconTable;

    /* ------------------------------------------------------------------ */
    /*  ⏱️ Timer UI                                                        */
    /* ------------------------------------------------------------------ */
    [Header("Timer")]
    [SerializeField] private TMP_Text _timerText;
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _yellowColor = Color.yellow;
    [SerializeField] private Color _orangeColor = new Color(1f, 0.55f, 0f);
    [SerializeField] private Color _redColor = Color.red;
    [SerializeField] private float _startTime = 180f;

    private float _timeLeft;
    private bool _timerRunning;

    /* ------------------------------------------------------------------ */
    /*  🏆 Score UI                                                      */
    /* ------------------------------------------------------------------ */
    [Header("Score")]
    [SerializeField] private TMP_Text _scoreText;

    /* ------------------------------------------------------------------ */
    /*  Internal cache                                                    */
    /* ------------------------------------------------------------------ */
    private BoxSO _cachedCurBox;
    private BoxSO _cachedNextBox;

    private bool _finishTriggered;  

    /* ------------------------------------------------------------------ */
    /*  Unity lifecycle                                                   */
    /* ------------------------------------------------------------------ */

    
    private void Start()
    {
        _finishTriggered = false;

        _timeLeft = _startTime;
        _timerRunning = true;

        ApplyWind(WindManager.Instance?.currentWind ?? Vector3.zero);
        RefreshBoxHUD();

        if (WindManager.Instance != null)
            WindManager.Instance.OnWindChanged += ApplyWind;

        /* -------- score: subscribe & initial draw -------- */
        if (ScoreManager.Instance != null)
        {
            // 1) initial value
            _scoreText.text = $"{ScoreManager.Instance.totalScore:0}";
            // 2) subscribe for future changes
            ScoreManager.Instance.OnScoreChanged += OnScoreChanged;
        }
    }

    private void OnEnable()
    {
        if (ScoreManager.Instance == null)
        {
            Debug.LogError("HUDManager: ScoreManager missing!");
            enabled = false;
            return;
        }
    }

    private void OnDestroy()
    {
        if (WindManager.Instance != null)
            WindManager.Instance.OnWindChanged -= ApplyWind;

        /* -------- score unsubscribe -------- */
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged -= OnScoreChanged;
    }

    private void Update()
    {
        /* ---- Timer update -------------------------------------------- */
        if (_timerRunning)
        {
            _timeLeft -= Time.deltaTime;
            if (_timeLeft <= 0f)
            {
                _timeLeft = 0f;
                _timerRunning = false;
                if (!_finishTriggered)
                {
                    _finishTriggered = true;
                    if (GameManager.Instance != null)
                        GameManager.Instance.FinishGame();
                }
            }
            UpdateTimerUI();
        }

        /* ---- Box HUD polling ----------------------------------------- */
        if (_player != null &&
            (_player.currentBox != _cachedCurBox || _player.nextBox != _cachedNextBox))
        {
            RefreshBoxHUD();
        }
    }

    /* ================================================================== */
    /*  Event callback for score                                          */
    /* ================================================================== */
    /// <summary>
    /// Updates score text whenever the global score changes.
    /// </summary>
    private void OnScoreChanged(float newTotal)
    {
        _scoreText.text = $"{newTotal:0}";
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
        foreach (var p in _iconTable)
            if (Mathf.Approximately(p.weight, weight))
                return p.icon;
        return null;
    }

    /* ------------------------------------------------------------------ */
    /*  Timer helpers                                                     */
    /* ------------------------------------------------------------------ */
    private void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(_timeLeft / 60f);
        int seconds = Mathf.FloorToInt(_timeLeft % 60f);
        _timerText.text = $"{minutes:0}:{seconds:00}";

        if (_timeLeft <= 30f)
            _timerText.color = _redColor;
        else if (_timeLeft <= 60f)
            _timerText.color = _orangeColor;
        else if (_timeLeft <= 120f)
            _timerText.color = _yellowColor;
        else
            _timerText.color = _normalColor;
    }
}
