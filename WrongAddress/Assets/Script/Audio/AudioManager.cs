using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Central audio hub: clip database, mixer control, volume sliders
/// </summary>
public class AudioManager : AudioSingleton<AudioManager>
{
    /* ------------------------------------------------------------------ */
    /*  Mixer references                                                  */
    /* ------------------------------------------------------------------ */
    [Header("Mixer Groups")]
    [SerializeField] private AudioMixerGroup masterMixer;
    [SerializeField] private AudioMixerGroup musicMixer;
    [SerializeField] private AudioMixerGroup sfxMixer;

    /* ------------------------------------------------------------------ */
    /*  Audio clip database (optional)                                    */
    /* ------------------------------------------------------------------ */
    [Header("Audio Clips")]
    [SerializeField] private List<AudioClip> musicClips = new List<AudioClip>();
    [SerializeField] private List<AudioClip> sfxClips = new List<AudioClip>();

    private readonly Dictionary<string, AudioClip> musicDict = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, AudioClip> sfxDict = new Dictionary<string, AudioClip>();

    /* ------------------------------------------------------------------ */
    /*  Cached slider values (0‑1)                                        */
    /* ------------------------------------------------------------------ */
    private float masterVol = 1f;
    private float musicVol = 1f;
    private float sfxVol = 1f;

    /* ------------------------------------------------------------------ */
    /*  Unity lifecycle                                                   */
    /* ------------------------------------------------------------------ */
    protected override void Awake()
    {
        base.Awake();          // duplicate‑safe singleton init
        CacheClips();          // build dictionaries
        LoadVolumes();         // restore saved slider values
        ApplyAllVolumes();     // push to mixer
    }

    /* ------------------------------------------------------------------ */
    /*  Clip helpers                                                      */
    /* ------------------------------------------------------------------ */
    private void CacheClips()
    {
        foreach (AudioClip c in musicClips)
            if (!musicDict.ContainsKey(c.name)) musicDict.Add(c.name, c);

        foreach (AudioClip c in sfxClips)
            if (!sfxDict.ContainsKey(c.name)) sfxDict.Add(c.name, c);
    }

    public AudioClip GetMusic(string clipName) => musicDict.TryGetValue(clipName, out var c) ? c : null;
    public AudioClip GetSfx(string clipName) => sfxDict.TryGetValue(clipName, out var c) ? c : null;

    /* ------------------------------------------------------------------ */
    /*  Volume control                                                    */
    /* ------------------------------------------------------------------ */
    /// <summary>Low‑level mixer setter (0‑1 range).</summary>
    public void SetVolume(AudioChannel ch, float value)
    {
        float db = AudioUtils.SliderToDb(Mathf.Clamp01(value));

        switch (ch)
        {
            case AudioChannel.Master:
                masterMixer.audioMixer.SetFloat("VolumeMaster", db);
                masterVol = value;
                break;
            case AudioChannel.Music:
                musicMixer.audioMixer.SetFloat("VolumeMusic", db);
                musicVol = value;
                break;
            case AudioChannel.Sfx:
                sfxMixer.audioMixer.SetFloat("VolumeSFX", db);
                sfxVol = value;
                break;
        }
    }

    private void ApplyAllVolumes()
    {
        SetVolume(AudioChannel.Master, masterVol);
        SetVolume(AudioChannel.Music, musicVol);
        SetVolume(AudioChannel.Sfx, sfxVol);
    }

    /* ------------------------------------------------------------------ */
    /*  Slider callbacks (UI hookup)                                      */
    /* ------------------------------------------------------------------ */
    /// <summary>UI slider → Master volume</summary>

    /// <summary>
    /// Returns the current cached 0-1 volume value for the requested channel.
    /// Useful for initializing sliders when (re)entering a scene.
    /// </summary>
    public float GetVolume01(AudioChannel ch)
    {
        switch (ch)
        {
            case AudioChannel.Master: return masterVol;
            case AudioChannel.Music: return musicVol;
            case AudioChannel.Sfx: return sfxVol;
            default: return 1f;
        }
    }

    public void OnMasterSlider(float value) => SetVolume(AudioChannel.Master, value);

    /// <summary>UI slider → Music volume</summary>
    public void OnMusicSlider(float value) => SetVolume(AudioChannel.Music, value);

    /// <summary>UI slider → SFX volume</summary>
    public void OnSfxSlider(float value) => SetVolume(AudioChannel.Sfx, value);

    /* ------------------------------------------------------------------ */
    /*  Persistent save / load                                            */
    /* ------------------------------------------------------------------ */
    private const string KeyMaster = "Audio_Master";
    private const string KeyMusic = "Audio_Music";
    private const string KeySfx = "Audio_Sfx";

    public void SaveVolumes()
    {
        PlayerPrefs.SetFloat(KeyMaster, masterVol);
        PlayerPrefs.SetFloat(KeyMusic, musicVol);
        PlayerPrefs.SetFloat(KeySfx, sfxVol);
        PlayerPrefs.Save();
    }

    private void LoadVolumes()
    {
        masterVol = PlayerPrefs.GetFloat(KeyMaster, 1f);
        musicVol = PlayerPrefs.GetFloat(KeyMusic, 1f);
        sfxVol = PlayerPrefs.GetFloat(KeySfx, 1f);
    }

    private void OnApplicationQuit() => SaveVolumes();
}

/// <summary>Logical mixer channels</summary>
public enum AudioChannel { Master, Music, Sfx }
