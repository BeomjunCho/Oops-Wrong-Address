using UnityEngine;

/// <summary>
/// Holds prefab variations for a given tile type.
/// </summary>
[CreateAssetMenu(menuName = "Runner/Tile Set", fileName = "TileSet_")]
public class TileSetSO : ScriptableObject
{
    public TileType tileType;
    public GameObject[] prefabs;
    [Range(1, 100)] public int spawnWeight = 10;
}

public enum TileType { Road, Grass, House }
