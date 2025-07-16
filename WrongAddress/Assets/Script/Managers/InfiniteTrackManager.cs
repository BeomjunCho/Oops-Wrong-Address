using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns 3-lane tiles endlessly: center Road, left/right Grass or House.
/// Keeps N rows ahead, recycles rows player passed, and enforces house gap.
/// Right-side tiles are rotated +180° so that “left-oriented” prefabs fit.
/// </summary>
public class InfiniteTrackManager : MonoBehaviour
{
    /* ------------------------------------------------------------------ */
    /*  Inspector                                                         */
    /* ------------------------------------------------------------------ */
    [Header("Tile Sets")]
    [SerializeField] private TileSetSO _roadSet;
    [SerializeField] private TileSetSO _grassSet;
    [SerializeField] private TileSetSO _houseSet;

    [Header("Params")]
    [SerializeField] private Transform _player;
    [SerializeField] private int _bufferRowsAhead = 6;   // rows kept in front
    [SerializeField] private int _recycleOffset = 3;   // rows behind tossed
    [SerializeField] private float _tileWidth = 30f;    // X size
    [SerializeField] private float _tileLength = 60f;    // Z size
    [Tooltip("Min Grass rows between Houses.")]
    [SerializeField] private int _houseGapMin = 2;
    [Tooltip("Probability (0-1) that a House spawns when allowed.")]
    [SerializeField] private float _houseProb = 0.35f;

    /* ------------------------------------------------------------------ */
    /*  Internal state                                                    */
    /* ------------------------------------------------------------------ */
    private readonly Dictionary<int, TileRow> _activeRows = new();
    private int _spawnedMaxRow;
    private int _lastHouseRowLeft;
    private int _lastHouseRowRight;

    /* ================================================================== */
    /*  Unity lifecycle                                                   */
    /* ================================================================== */
    private void Start()
    {
        int curRow = RowIndex(_player.position.z);

        // Initialize house gap so first eligible row
        _lastHouseRowLeft = curRow - _houseGapMin - 1;
        _lastHouseRowRight = curRow - _houseGapMin - 1;

        for (int r = curRow; r < curRow + _bufferRowsAhead; ++r)
            SpawnRow(r);

        _spawnedMaxRow = curRow + _bufferRowsAhead - 1;
    }

    private void Update()
    {
        int playerRow = RowIndex(_player.position.z);

        /* ---------- spawn ahead ---------- */
        while (_spawnedMaxRow < playerRow + _bufferRowsAhead - 1)
        {
            ++_spawnedMaxRow;
            SpawnRow(_spawnedMaxRow);
        }

        /* ---------- recycle behind ---------- */
        int recycleRow = playerRow - _recycleOffset;
        if (_activeRows.TryGetValue(recycleRow, out var row))
        {
            DespawnRow(row);
            _activeRows.Remove(recycleRow);
        }
    }

    /* ================================================================== */
    /*  Row helpers                                                       */
    /* ================================================================== */

    public void ResetTrack()
    {
        // Return every active row's tiles to the pool.
        foreach (var kvp in _activeRows)
            DespawnRow(kvp.Value);
        
        _activeRows.Clear();
        
        // Stop Update() spawning until this scene instance is destroyed.
        enabled = false;
    }
    private int RowIndex(float z) => Mathf.FloorToInt(z / _tileLength);

    private void SpawnRow(int rowIdx)
    {
        var row = new TileRow
        {
            center = SpawnTile(_roadSet, 0, rowIdx, Quaternion.identity),
            left = SpawnSideTile(-1, ref _lastHouseRowLeft, rowIdx),
            right = SpawnSideTile(+1, ref _lastHouseRowRight, rowIdx, true)
        };
        _activeRows[rowIdx] = row;
    }

    private GameObject SpawnSideTile(int col,
                                     ref int lastHouseRow,
                                     int rowIdx,
                                     bool rotate180 = false)
    {
        bool canHouse = rowIdx >= lastHouseRow + _houseGapMin + 1;
        bool placeHouse = canHouse && Random.value < _houseProb;

        TileSetSO set = placeHouse ? _houseSet : _grassSet;
        if (placeHouse) lastHouseRow = rowIdx;

        Quaternion rot = rotate180 ? Quaternion.Euler(0f, 180f, 0f)
                                   : Quaternion.identity;

        return SpawnTile(set, col, rowIdx, rot);
    }

    private GameObject SpawnTile(TileSetSO set,
                                 int col,
                                 int rowIdx,
                                 Quaternion rotation)
    {
        GameObject prefab = set.prefabs[Random.Range(0, set.prefabs.Length)];
        GameObject go = TilePool.Instance.Rent(prefab);

        Vector3 pos = new(col * _tileWidth, 0f, rowIdx * _tileLength);
        go.transform.SetPositionAndRotation(pos, rotation);
        return go;
    }

    private void DespawnRow(TileRow row)
    {
        ReturnTile(row.center);
        ReturnTile(row.left);
        ReturnTile(row.right);
    }

    private void ReturnTile(GameObject obj)
    {
        if (obj == null) return;
        TilePool.Instance.Return(obj);
    }

    /* ================================================================== */
    /*  Data                                                              */
    /* ================================================================== */
    private struct TileRow
    {
        public GameObject center, left, right;
    }
}
