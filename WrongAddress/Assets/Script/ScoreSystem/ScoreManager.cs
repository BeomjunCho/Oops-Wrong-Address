using System;
using UnityEngine;

/// <summary>
/// Global score keeper (singleton).  
/// Call AddScore to increase points; subscribable event notifies HUD.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    /* ------------------------------------------------------------------ */
    /*  Singleton setup                                                   */
    /* ------------------------------------------------------------------ */
    public static ScoreManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /* ------------------------------------------------------------------ */
    /*  Scores + event                                                    */
    /* ------------------------------------------------------------------ */
    private float _totalScore;
    /// <summary>Total accumulated score.</summary>
    public float totalScore => _totalScore;

    /// <summary>Event fired when score changes. Arg = new total score.</summary>
    public event Action<float> OnScoreChanged;
    public event Action<float, Vector3> OnScorePopup; // amount, worldPos

    /// <summary>
    /// Adds points to the total score.
    /// </summary>
    /// <param name="amount">Points to add (may be zero).</param>
    /// <param name="worldPos">World position where the score was awarded (for pop-ups).</param>
    public void AddScore(float amount, Vector3 worldPos)
    {
        Debug.Log($"AddScore called: {amount}", this);
        if (amount <= 0f) return;

        OnScorePopup?.Invoke(amount, worldPos);      // popup first

        _totalScore += amount;
        OnScoreChanged?.Invoke(_totalScore);
    }
}
