using UnityEngine;

/// <summary>
/// Simple screen switcher that enables exactly one child screen at a time.
/// Usage:
///   uiManager.ShowScreen(UIScreenType.Paused);
/// </summary>
public enum UIScreenType { HUD, Paused, Finished }

public class UIScreenManager : MonoBehaviour
{
    [Header("Screen References")]
    [SerializeField] private GameObject _hudScreen;
    [SerializeField] private GameObject _pausedScreen;
    [SerializeField] private GameObject _finishedScreen;

    private UIScreenType _current;

    private void Awake() => ShowScreen(UIScreenType.HUD);

    /// <summary>Enables the requested screen and disables the others.</summary>
    public void ShowScreen(UIScreenType type)
    {
        _current = type;

        _hudScreen.SetActive(type == UIScreenType.HUD);
        _pausedScreen.SetActive(type == UIScreenType.Paused);
        _finishedScreen.SetActive(type == UIScreenType.Finished);
    }

    /// <summary>Current active screen.</summary>
    public UIScreenType currentScreen => _current;
}
