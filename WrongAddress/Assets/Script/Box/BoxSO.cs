using UnityEngine;

/// <summary>
/// Defines a throwable box type (prefab + weight).
/// </summary>
[CreateAssetMenu(menuName = "Runner/Box", fileName = "Box_")]
public class BoxSO : ScriptableObject
{
    public GameObject prefab;
    [Min(0.1f)] public float weight = 1f;   // kg, affects physics & player
}
