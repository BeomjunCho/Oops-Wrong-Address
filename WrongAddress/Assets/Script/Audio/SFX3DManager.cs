// SFX3DManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 3D sound-effects manager with pooling and distance-based spatial blend
/// </summary>
public class SFX3DManager : AudioSingleton<SFX3DManager>
{
    // Inspector settings
    [Header("3D Audio Settings")]
    public int maxPoolSize = 10;
    public float defaultMinDistance = 1f;
    public float defaultMaxDistance = 20f;
    public float defaultDopplerLevel = 1f;
    public float defaultSpread = 0f;
    public AudioMixerGroup sfx3dMixerGroup;

    // Internal state
    private readonly List<AudioSource> audioSourcePool = new List<AudioSource>();
    private readonly Dictionary<string, AudioSource> active3dSounds = new Dictionary<string, AudioSource>();
    private Camera cachedCamera;

#if UNITY_EDITOR
    private void OnDestroy()
    {
        // Cleanup persistent audio sources and manager when exiting Play Mode in Editor
        if (!Application.isPlaying)
        {
            // Destroy all pooled AudioSource GameObjects
            for (int i = transform.childCount - 1; i >= 0; --i)
                DestroyImmediate(transform.GetChild(i).gameObject);
            // Destroy this manager GameObject
            DestroyImmediate(gameObject);
        }
    }
#endif

    // Awake override
    protected override void Awake()
    {
        base.Awake(); // call base awake for singleton init

        cachedCamera = Camera.main;
        if (cachedCamera == null)
            Debug.LogWarning("SFX3DManager: No main camera found. Spatial blend adjustment may not work.");

        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < maxPoolSize; i++)
            CreateAudioSource("3D_AudioSource_" + i);
    }

    private AudioSource CreateAudioSource(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);

        AudioSource src = go.AddComponent<AudioSource>();
        src.spatialBlend = 1f;
        src.playOnAwake = false;
        src.loop = false;
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.minDistance = defaultMinDistance;
        src.maxDistance = defaultMaxDistance;
        if (sfx3dMixerGroup != null)
            src.outputAudioMixerGroup = sfx3dMixerGroup;

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
            AudioSource extra = CreateAudioSource("3D_AudioSource_Extra_" + audioSourcePool.Count);
            ResetAudioSource(extra);
            return extra;
        }

        Debug.LogWarning("SFX3DManager: No available 3D audio source in pool");
        return null;
    }

    private void ResetAudioSource(AudioSource src)
    {
        src.pitch = 1f;
        src.spatialBlend = 1f;
        src.dopplerLevel = defaultDopplerLevel;
        src.spread = defaultSpread;
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.minDistance = defaultMinDistance;
        src.maxDistance = defaultMaxDistance;
    }

    /// <summary>
    /// Play a 3D one-shot or looping sound at the given spawn transform
    /// </summary>
    public void Play3dSfx(string soundName, AudioClip clip, Transform spawn, float volume, bool loop = false, float? minDist = null, float? maxDist = null)
    {
        AudioSource src = GetAvailableAudioSource();
        if (src == null) return;

        float minD = minDist ?? defaultMinDistance;
        float maxD = maxDist ?? defaultMaxDistance;

        src.transform.SetParent(spawn, false);
        src.transform.localPosition = Vector3.zero;
        src.clip = clip;
        src.volume = volume;
        src.loop = loop;
        src.minDistance = minD;
        src.maxDistance = maxD;
        src.gameObject.SetActive(true);
        src.Play();

        if (loop)
        {
            active3dSounds[soundName] = src;
        }
        else
        {
            string key = soundName + "_" + Time.time;
            active3dSounds[key] = src;
            StartCoroutine(DeactivateAfterDuration(key, clip.length));
        }
    }

    /// <summary>
    /// Play looping ambience with automatic spatial blend adjustment
    /// </summary>
    public void Play3dAmbience(string soundName, AudioClip clip, Transform spawn, float volume, float minDistance = 1f, float maxDistance = 50f)
    {
        Play3dSfx(soundName, clip, spawn, volume, true, minDistance, maxDistance);

        if (active3dSounds.TryGetValue(soundName, out AudioSource ambience))
        {
            ambience.dopplerLevel = 0f;
            StartCoroutine(AdjustSpatialBlendOverDistance(ambience, minDistance, maxDistance));
        }
    }

    private IEnumerator AdjustSpatialBlendOverDistance(AudioSource src, float minDistance, float maxDistance)
    {
        while (src != null && src.isPlaying)
        {
            float dist = Vector3.Distance(src.transform.position, cachedCamera.transform.position);
            src.spatialBlend = (dist <= minDistance) ? 0f : Mathf.Clamp01((dist - minDistance) / (maxDistance - minDistance));
            yield return new WaitForSeconds(0.2f);
        }
    }

    private IEnumerator DeactivateAfterDuration(string key, float duration)
    {
        yield return new WaitForSeconds(duration);
        if (active3dSounds.TryGetValue(key, out AudioSource src))
        {
            src.Stop();
            src.transform.SetParent(transform, false);
            src.gameObject.SetActive(false);
            active3dSounds.Remove(key);
        }
    }

    /// <summary>
    /// Stop all sounds that start with the given name prefix
    /// </summary>
    public void Stop3dSound(string soundName)
    {
        List<string> toRemove = new List<string>();
        foreach (var kvp in active3dSounds)
        {
            if (kvp.Key.StartsWith(soundName))
            {
                AudioSource src = kvp.Value;
                src.Stop();
                src.transform.SetParent(transform, false);
                src.gameObject.SetActive(false);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (string key in toRemove)
            active3dSounds.Remove(key);
    }

    /// <summary>
    /// Stop all active 3D sounds
    /// </summary>
    public void StopAll3dSounds()
    {
        foreach (var kvp in active3dSounds)
        {
            kvp.Value.Stop();
            kvp.Value.transform.SetParent(transform, false);
            kvp.Value.gameObject.SetActive(false);
        }
        active3dSounds.Clear();
    }

    /// <summary>
    /// Set volume for a specific 3D sound
    /// </summary>
    public void Set3dSoundVolume(string soundName, float volume)
    {
        if (active3dSounds.TryGetValue(soundName, out AudioSource src))
            src.volume = volume;
        else
            Debug.LogWarning("SFX3DManager: No active sound '" + soundName + "'");
    }

    /// <summary>
    /// Adjust the 3D mixer group volume using a slider value (0-1)
    /// </summary>
    public void Adjust3DAudioMixerVolume(float sliderValue)
    {
        if (sfx3dMixerGroup != null && sfx3dMixerGroup.audioMixer != null)
            sfx3dMixerGroup.audioMixer.SetFloat("Volume", AudioUtils.SliderToDb(sliderValue));
    }
}
