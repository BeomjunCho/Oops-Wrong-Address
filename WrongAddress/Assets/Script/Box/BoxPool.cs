using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic object pool for tile prefabs.
/// Pre-warms and reuses instances.
/// </summary>
public class BoxPool : MonoBehaviour
{
    public static BoxPool Instance { get; private set; }

    [SerializeField] private int _prewarmPerPrefab = 6;

    private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public GameObject Rent(GameObject prefab)
    {
        if (!_pools.TryGetValue(prefab, out var q))
        {
            q = new Queue<GameObject>();
            _pools[prefab] = q;
            Prewarm(prefab, q);
        }

        if (q.Count == 0) Prewarm(prefab, q);

        GameObject go = q.Dequeue();
        go.SetActive(true);
        return go;
    }

    public void Return(GameObject obj)
    {
        var pr = obj.GetComponent<PooledRoot>();
        if (pr == null || pr.sourcePrefab == null)
        {
            Debug.LogError($"Pooled object '{obj.name}' missing PooledRoot / sourcePrefab!");
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        _pools[pr.sourcePrefab].Enqueue(obj);
    }

    private void Prewarm(GameObject prefab, Queue<GameObject> q)
    {
        for (int i = 0; i < _prewarmPerPrefab; ++i)
        {
            GameObject go = Instantiate(prefab, transform);
            var pr = go.GetComponent<PooledRoot>() ?? go.AddComponent<PooledRoot>();
            if (pr.sourcePrefab == null) pr.sourcePrefab = prefab;   // set source prefab
            go.SetActive(false);
            q.Enqueue(go);
        }
    }
}
