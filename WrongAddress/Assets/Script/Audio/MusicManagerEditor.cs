#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Runtime debug inspector for MusicManager.
/// </summary>
[CustomEditor(typeof(MusicManager))]
public class MusicManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (!Application.isPlaying) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Runtime Debug", EditorStyles.boldLabel);

        MusicManager mgr = (MusicManager)target;

        if (GUILayout.Button("Stop"))
            mgr.StopMusic();

        foreach (MusicTrack track in System.Enum.GetValues(typeof(MusicTrack)))
        {
            if (GUILayout.Button($"Play {track}"))
                mgr.PlayMusicByEnum(track, 0.5f, true, 0.5f);
        }

        float newVol = EditorGUILayout.Slider("Volume", mgr.GetComponentInChildren<AudioSource>().volume, 0f, 1f);
        if (GUILayout.Button("Apply Volume"))
            mgr.FadeToVolume(newVol, 0.2f);
    }
}
#endif
