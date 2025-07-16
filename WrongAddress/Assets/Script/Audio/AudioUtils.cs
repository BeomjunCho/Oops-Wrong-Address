using UnityEngine;

/// <summary>Linear–dB conversion helpers for UI sliders.</summary>
public static class AudioUtils
{
    private const float MinDb = -80f; // Unity mute
    private const float MaxDb = 0f;   // Full volume

    public static float SliderToDb(float slider)
    {
        const float minDb = -80f;      // mute
        const float maxDb = 0f;        // full
        float clamped = Mathf.Clamp(slider, 0.0001f, 1f); 
        float db = Mathf.Log10(clamped) * 20f;           
        return Mathf.Clamp(db, minDb, maxDb);
    }

    public static float DbToSlider(float db) =>
        Mathf.InverseLerp(MinDb, MaxDb, db);
}
