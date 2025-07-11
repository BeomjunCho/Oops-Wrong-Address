using UnityEngine;

/// <summary>
/// Stores reference to the original prefab so the pool can identify it
/// when returning an instance.
/// </summary>
[DisallowMultipleComponent]
public sealed class PooledRoot : MonoBehaviour
{
    [Tooltip("Reference to the prefab this instance was created from.")]
    [HideInInspector] public GameObject sourcePrefab;

#if UNITY_EDITOR
    /* -------------------------------------------------------------- */
    /*  Auto-assign in editor (quality-of-life)                       */
    /* -------------------------------------------------------------- */
    private void OnValidate()
    {
        if (sourcePrefab == null)
        {
            // Try to assign prefab asset automatically when edited
            var prefabAsset = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
            if (prefabAsset != null)
                sourcePrefab = prefabAsset;
        }
    }
#endif
}
