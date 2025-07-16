// SFX2DManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 2D sound-effects manager with simple pooling
/// </summary>
public class SFX2DManager : AudioSingleton<SFX2DManager>
{
    // Inspector settings
    [Header("2D Audio Settings")]
    public int maxPoolSize = 10;
    public AudioMixerGroup sfx2dMixerGroup;

    // Internal state
    private readonly List<AudioSource> audioSourcePool = new List<AudioSource>();
    private readonly Dictionary<string, AudioSource> active2dSounds = new Dictionary<string, AudioSource>();

    // Awake override
    protected override void Awake()
    {
        base.Awake(); // call base awake for singleton init
        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < maxPoolSize; i++)
            CreateAudioSource("2D_AudioSource_" + i);
    }

    private AudioSource CreateAudioSource(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);

        AudioSource src = go.AddComponent<AudioSource>();
        src.spatialBlend = 0f;
        src.playOnAwake = false;
        src.loop = false;
        if (sfx2dMixerGroup != null)
            src.outputAudioMixerGroup = sfx2dMixerGroup;

        go.SetActive(false);
        audioSourcePool.Add(src);
        return src;
    }

    private AudioSource GetAvailableAudioSource()
    {
        foreach (AudioSource src in audioSourcePool)
        {
            if (!src.isPlaying)
            {
                ResetAudioSource(src);
                return src;
            }
        }
        if (audioSourcePool.Count < maxPoolSize * 2)
        {
            AudioSource extra = CreateAudioSource("2D_AudioSource_Extra_" + audioSourcePool.Count);
            ResetAudioSource(extra);
            return extra;
        }
        Debug.LogWarning("SFX2DManager: No available 2D audio source in pool");
        return null;
    }

    private static void ResetAudioSource(AudioSource src)
    {
        src.pitch = 1f;
        src.spatialBlend = 0f;
    }

    /// <summary>
    /// Play a 2D one-shot sound
    /// </summary>
    public void Play2dSfx(string soundName, AudioClip clip, float volume)
    {
        AudioSource src = GetAvailableAudioSource();
        if (src == null) return;

        src.clip = clip;
        src.volume = volume;
        src.loop = false;
        src.gameObject.SetActive(true);
        src.Play();

        string key = soundName + "_" + Time.time;
        active2dSounds[key] = src;
        StartCoroutine(DeactivateAfterDuration(key, clip.length));
    }

    private IEnumerator DeactivateAfterDuration(string key, float duration)
    {
        yield return new WaitForSeconds(duration);
        if (active2dSounds.TryGetValue(key, out AudioSource src))
        {
            src.Stop();
            src.gameObject.SetActive(false);
            active2dSounds.Remove(key);
        }
    }

    /// <summary>
    /// Stop all sounds that start with the given name prefix
    /// </summary>
    public void Stop2dSound(string soundName)
    {
        List<string> toRemove = new List<string>();
        foreach (var kvp in active2dSounds)
        {
            if (kvp.Key.StartsWith(soundName))
            {
                AudioSource src = kvp.Value;
                src.Stop();
                src.gameObject.SetActive(false);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (string key in toRemove)
            active2dSounds.Remove(key);
    }

    /// <summary>
    /// Stop all active 2D sounds
    /// </summary>
    public void StopAll2dSounds()
    {
        foreach (var kvp in active2dSounds)
        {
            kvp.Value.Stop();
            kvp.Value.gameObject.SetActive(false);
        }
        active2dSounds.Clear();
    }

    /// <summary>
    /// Set volume for a specific 2D sound
    /// </summary>
    public void Set2dSoundVolume(string soundName, float volume)
    {
        if (active2dSounds.TryGetValue(soundName, out AudioSource src))
            src.volume = volume;
        else
            Debug.LogWarning("SFX2DManager: No active sound '" + soundName + "'");
    }

    /// <summary>
    /// Adjust the 2D mixer group volume using a slider value (0-1)
    /// </summary>
    public void Adjust2DAudioMixerVolume(float sliderValue)
    {
        if (sfx2dMixerGroup != null && sfx2dMixerGroup.audioMixer != null)
            sfx2dMixerGroup.audioMixer.SetFloat("Volume", AudioUtils.SliderToDb(sliderValue));
    }
}
