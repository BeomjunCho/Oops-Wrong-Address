using UnityEngine;

/// <summary>
/// Simple controller for the main-menu canvas.  
/// Toggles the Settings panel on / off. Attach this to the canvas root.
/// </summary>
public class MainMenuCanvas : MonoBehaviour
{
    [Header("Panel References")]
    [Tooltip("Panel that contains Settings UI.")]
    [SerializeField] private GameObject _settingsPanel;
    [SerializeField] private GameObject _controlPanel;

    /// <summary>
    /// Called by 'Settings' button. Toggles visibility.
    /// </summary>
    public void ToggleSettings()
    {
        if (_settingsPanel == null) return;

        bool isActive = _settingsPanel.activeSelf;
        _settingsPanel.SetActive(!isActive);
    }

    public void Togglecontrol()
    {
        if (_controlPanel == null) return;

        bool isActive = _controlPanel.activeSelf;
        _controlPanel.SetActive(!isActive);
    }

    /// <summary>
    /// Optional helper: ensure panel is hidden when scene starts.
    /// </summary>
    private void Awake()
    {
        if (_settingsPanel != null)
            _settingsPanel.SetActive(false);
    }
}
