using System;
using UnityEngine;

/// <summary>
/// Global score keeper (singleton).  
/// Call AddScore to increase points; subscribable event notifies HUD.
/// </summary>
[DefaultExecutionOrder(-200)]   // ScoreManager first
public class ScoreManager : MonoBehaviour
{
    /* ------------------------------------------------------------------ */
    /*  Singleton setup                                                   */
    /* ------------------------------------------------------------------ */
    public static ScoreManager Instance { get; private set; }

    /* ------------------------------------------------------------------ */
    /*  Scores + event                                                    */
    /* ------------------------------------------------------------------ */
    private const string BestKey = "BEST_SCORE";

    private float _totalScore;
    /// <summary>Total accumulated score.</summary>
    public float totalScore => _totalScore;

    private float _bestScore;
    public float bestScore => _bestScore;

    /// <summary>Event fired when score changes. Arg = new total score.</summary>
    public event Action<float> OnScoreChanged;
    public event Action<float, Vector3> OnScorePopup; // amount, worldPos


    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // load best score from disk
        _bestScore = PlayerPrefs.GetFloat(BestKey, 0f);
    }

    /// <summary>
    /// Adds points to the total score.
    /// </summary>
    /// <param name="amount">Points to add (may be zero).</param>
    /// <param name="worldPos">World position where the score was awarded (for pop-ups).</param>
    public void AddScore(float amount, Vector3 worldPos)
    {
        var scoreClip = AudioManager.Instance.GetSfx("Score_Up");
        SFX2DManager.Instance.Play2dSfx("Score_Up", scoreClip, 1.0f);

        Debug.Log($"AddScore called: {amount}", this);
        if (amount <= 0f) return;

        OnScorePopup?.Invoke(amount, worldPos);      // popup in score pop up spawner

        _totalScore += amount;
        OnScoreChanged?.Invoke(_totalScore);

        // update best score if needed
        if (_totalScore > _bestScore)
        {
            _bestScore = _totalScore;
            PlayerPrefs.SetFloat(BestKey, _bestScore);
            PlayerPrefs.Save();
        }
    }
    public void ResetScore()
    {
        _totalScore = 0f;
        OnScoreChanged?.Invoke(_totalScore);
    }
}
