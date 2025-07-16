using UnityEngine;

/// <summary>
/// Generic singleton base that survives scene loads
/// and destroys duplicate instances.
/// </summary>
public class AudioSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    /// <summary>Global access point.</summary>
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<T>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject(typeof(T).Name, typeof(T));
                    _instance = obj.GetComponent<T>();
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Duplicate-safe Awake.  
    /// </summary>
    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this as T;
        DontDestroyOnLoad(gameObject);
    }
}
