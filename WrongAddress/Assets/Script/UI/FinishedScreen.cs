using UnityEngine;
using TMPro;

/// <summary>
/// Displays final and best score when Finished panel activates and
/// toggles cursor visibility.
/// </summary>
public class FinishedScreen : MonoBehaviour
{
    [Header("Text refs")]
    [SerializeField] private TMP_Text _finalText;
    [SerializeField] private TMP_Text _bestText;
    [SerializeField] private TMP_Text _newBestTag; // optional "NEW BEST!" label

    /* ------------------------------------------------------------------ */
    /*  Unity callbacks                                                   */
    /* ------------------------------------------------------------------ */
    private void OnEnable()
    {
        // 1) update score texts
        float total = ScoreManager.Instance != null
                      ? ScoreManager.Instance.totalScore
                      : 0f;
        float best = ScoreManager.Instance != null
                      ? ScoreManager.Instance.bestScore
                      : 0f;

        if (_finalText != null) _finalText.text = $"{total:0}";
        if (_bestText != null) _bestText.text = $"{best:0}";

        bool isNew = Mathf.Approximately(total, best);
        if (_newBestTag != null) _newBestTag.gameObject.SetActive(isNew);

        // 2) show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnDisable()
    {
        // hide cursor again when the panel closes
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
