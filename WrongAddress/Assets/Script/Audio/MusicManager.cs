using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Central background?music controller:
/// ? enum?based track lookup
/// ? cross?fade playback
/// ? runtime volume & mixer control.
/// </summary>
public class MusicManager : AudioSingleton<MusicManager>
{
    /* ------------------------------------------------------------------ */
    /*  Inspector                                                         */
    /* ------------------------------------------------------------------ */
    [Header("Music Settings")]
    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioMixerGroup _musicMixerGroup;

    [System.Serializable]
    public struct MusicEntry
    {
        public MusicTrack trackType;
        public AudioClip clip;
    }

    [Header("Music Tracks")]
    [SerializeField] private List<MusicEntry> _musicTracks = new();

    /* ------------------------------------------------------------------ */
    /*  Internal state                                                    */
    /* ------------------------------------------------------------------ */
    private Dictionary<MusicTrack, AudioClip> _musicDictionary;
    private Coroutine _fadeRoutine;
    private float _targetVolume = 0.5f;

    /* ------------------------------------------------------------------ */
    /*  Unity lifecycle                                                   */
    /* ------------------------------------------------------------------ */
    protected override void Awake()
    {
        base.Awake(); // duplicate?check

        // Build or get AudioSource.
        if (_musicSource == null)
        {
            GameObject obj = new GameObject("MusicSource");
            obj.transform.SetParent(transform, false);
            _musicSource = obj.AddComponent<AudioSource>();
            _musicSource.spatialBlend = 0f;
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.bypassReverbZones = true;

            if (_musicMixerGroup != null)
                _musicSource.outputAudioMixerGroup = _musicMixerGroup;
        }

        // Convert list to dictionary for O(1) lookup.
        _musicDictionary = new Dictionary<MusicTrack, AudioClip>(_musicTracks.Count);
        foreach (var entry in _musicTracks)
        {
            if (!_musicDictionary.ContainsKey(entry.trackType))
                _musicDictionary.Add(entry.trackType, entry.clip);
        }
    }

    /* ------------------------------------------------------------------ */
    /*  Public API                                                        */
    /* ------------------------------------------------------------------ */

    /// <summary>Plays <paramref name="track"/> with optional cross?fade.</summary>
    public void PlayMusicByEnum(MusicTrack track,
                                float volume = 0.5f,
                                bool loop = true,
                                float fadeTime = 0.5f)
    {
        _targetVolume = Mathf.Clamp01(volume);

        if (_musicDictionary.TryGetValue(track, out var clip))
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(CrossfadeRoutine(clip, loop, fadeTime));
        }
        else
        {
            Debug.LogWarning($"MusicManager: Track not found {track}");
        }
    }

    /// <summary>Fades to silence and stops playback.</summary>
    public void StopMusic(float fadeTime = 0.3f)
    {
        if (!_musicSource.isPlaying) return;

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeVolumeRoutine(0f, fadeTime, () =>
        {
            _musicSource.Stop();
            _musicSource.clip = null;
        }));
    }

    /// <summary>Slides volume at runtime (0?1).</summary>
    public void FadeToVolume(float volume, float time = 0.3f)
    {
        _targetVolume = Mathf.Clamp01(volume);

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeVolumeRoutine(_targetVolume, time));
    }

    /// <summary>Updates mixer dB via slider value (0?1).</summary>
    public void SetMixerVolume(float sliderValue)
    {
        if (_musicMixerGroup == null || _musicMixerGroup.audioMixer == null) return;
        _musicMixerGroup.audioMixer.SetFloat("Volume", AudioUtils.SliderToDb(sliderValue));
    }

    /* ------------------------------------------------------------------ */
    /*  Private helpers                                                   */
    /* ------------------------------------------------------------------ */

    private IEnumerator CrossfadeRoutine(AudioClip newClip, bool loop, float time)
    {
        // Fade?out current.
        if (_musicSource.isPlaying)
            yield return FadeVolumeRoutine(0f, time * 0.5f);

        // Switch clip.
        _musicSource.clip = newClip;
        _musicSource.loop = loop;
        _musicSource.Play();

        // Fade?in.
        yield return FadeVolumeRoutine(_targetVolume, time * 0.5f);
    }

    private IEnumerator FadeVolumeRoutine(float target, float time, System.Action onComplete = null)
    {
        float start = _musicSource.volume;
        float elapsed = 0f;

        while (elapsed < time)
        {
            elapsed += Time.unscaledDeltaTime;
            _musicSource.volume = Mathf.Lerp(start, target, elapsed / time);
            yield return null;
        }

        _musicSource.volume = target;
        onComplete?.Invoke();
    }
}
