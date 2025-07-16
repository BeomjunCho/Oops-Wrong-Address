using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent manager that survives scene changes.
/// Handles StartGame, ReturnToMainMenu, Pause/Resume (Tab), and Finish.
/// UIScreenManager exists only in the InGame scene, so it is located
/// every time that scene is loaded.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /* ------------------------------------------------------------------ */
    /*  Fields                                                            */
    /* ------------------------------------------------------------------ */
    private UIScreenManager _ui;    // valid only in InGame
    private bool _isPaused;

    /* ---------- STATIC BUTTON WRAPPERS ---------- */
    public static void BtnStartGame() => Instance?.StartGame();
    public static void BtnReturnMenu() => Instance?.ReturnToMainMenu();
    public static void BtnReplay() => Instance?.ReplayGame();
    public static void BtnResume() => Instance?.ResumeGame();
    public static void BtnQuit() => Instance?.QuitGame();

    /* ------------------------------------------------------------------ */
    /*  Persistence                                                       */
    /* ------------------------------------------------------------------ */
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        DontDestroyOnLoad(gameObject);
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // locate UIScreenManager in the newly loaded scene (may be null)
        _ui = FindAnyObjectByType<UIScreenManager>();

        if (_ui == null)          // Main-Menu scene
        {
            MusicManager.Instance.PlayMusicByEnum(MusicTrack.MainMenu, 0.7f, true, 0.5f); // vol, loop, fade

            Time.timeScale = 1f;
            _isPaused = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else                      // In-Game scene (HUD active)
        {
            MusicManager.Instance.PlayMusicByEnum(MusicTrack.InGame, 0.6f, true, 1f);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /* ------------------------------------------------------------------ */
    /*  Update loop                                                       */
    /* ------------------------------------------------------------------ */
    private void Update()
    {
        // Tab toggles pause only when _ui is available (InGame)
        if (_ui != null && Input.GetKeyDown(KeyCode.Tab))
        {
            if (_isPaused) ResumeGame();
            else PauseGame();
        }
    }

    /* ------------------------------------------------------------------ */
    /*  Main-menu button                                                  */
    /* ------------------------------------------------------------------ */
    public void StartGame() // Play button
    {
        ResetSingletons();
        UnityEngine.SceneManagement.SceneManager.LoadScene("InGame");
    }

    /* ------------------------------------------------------------------ */
    /*  In-game buttons                                                   */
    /* ------------------------------------------------------------------ */
    public void ReturnToMainMenu() // Back-to-Menu button
    {
        ResetSingletons();
        Time.timeScale = 1f; // make sure game is not paused
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public void FinishGame() // called when the game ends
    {
        if (_ui == null) return;
        Time.timeScale = 0f;
        _ui.ShowScreen(UIScreenType.Finished);

        var finishClip = AudioManager.Instance.GetSfx("FinishGame");
        SFX2DManager.Instance.Play2dSfx("FinishGame", finishClip, 1.0f);
    }

    /// <summary>Called by Replay button on Finished screen.</summary>
    public void ReplayGame()
    {
        Time.timeScale = 1f;      // ensure unpaused
        _isPaused = false;
        StartGame();              // reuse existing startup logic
    }

    public void QuitGame()
    {
        // If we are running a standalone build, quit the application
        Application.Quit();

#if UNITY_EDITOR
        // If we are in the Editor, stop Play Mode
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    /* ------------------------------------------------------------------ */
    /*  Pause helpers                                                     */
    /* ------------------------------------------------------------------ */
    private void PauseGame()
    {
        if (_ui == null) return;
        Time.timeScale = 0f;
        _ui.ShowScreen(UIScreenType.Paused);
        Cursor.lockState = CursorLockMode.None;   // show mouse
        Cursor.visible = true;
        _isPaused = true;
    }

    public void ResumeGame() // linked to Resume button
    {
        if (_ui == null) return;
        Time.timeScale = 1f;
        _ui.ShowScreen(UIScreenType.HUD);
        Cursor.lockState = CursorLockMode.Locked; // hide mouse
        Cursor.visible = false;
        _isPaused = false;
    }

    /* ------------------------------------------------------------------ */
    /*  Utility                                                           */
    /* ------------------------------------------------------------------ */
    private static void ResetSingletons()
    {
        var track = Object.FindAnyObjectByType<InfiniteTrackManager>();
            if (track != null)
            track.ResetTrack();

        if (BoxPool.Instance != null) BoxPool.Instance.ClearPools();
        if (TilePool.Instance != null) TilePool.Instance.ClearPools();
        if (ScoreManager.Instance != null) ScoreManager.Instance.ResetScore();
        if (WindManager.Instance != null) WindManager.Instance.ResetWind();
    }
}
