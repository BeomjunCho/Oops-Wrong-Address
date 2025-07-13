using System.Collections;
using UnityEngine;
using TMPro;

public class ScorePopupSpawner : MonoBehaviour
{
    [SerializeField] private Camera _worldCam;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private TMP_Text _popupPrefab;
    [SerializeField] private float _riseDistance = 60f;
    [SerializeField] private float _duration = 1.2f;

    private bool _subscribed;

    private void Awake()
    {
        if (_worldCam == null) _worldCam = Camera.main;
        if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
    }

    private void Start()
    {
        // wait until ScoreManager is alive, then subscribe exactly once
        StartCoroutine(SubscribeWhenReady());
    }

    private IEnumerator SubscribeWhenReady()
    {
        while (ScoreManager.Instance == null) yield return null;
        if (_subscribed) yield break;                   // already done

        ScoreManager.Instance.OnScorePopup += SpawnPopup;
        _subscribed = true;
    }

    private void OnDisable()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScorePopup -= SpawnPopup;
    }

    private void SpawnPopup(float amount, Vector3 worldPos)
    {
        /* -------------------------------------------------------------- */
        /* 1) world ¡æ viewport coordinates (0..1). z tells front / back.  */
        /* -------------------------------------------------------------- */
        Vector3 view = _worldCam.WorldToViewportPoint(worldPos);

        /* If the object is behind the camera (z < 0), mirror X/Y */
        if (view.z < 0f)
        {
            view.x = 1f - view.x;
            view.y = 1f - view.y;
            view.z = 0.01f;               // push slightly in front
        }

        /* -------------------------------------------------------------- */
        /* 2) Clamp to left/right edge, Y = center (0.5).                 */
        /* -------------------------------------------------------------- */
        const float edgeX = 0.08f;        // 3 % from edge

        if (view.x < 0f)                  // left out of bounds
        {
            view.x = edgeX;
            view.y = 0.5f;                // vertical middle
        }
        else if (view.x > 1f)             // right out of bounds
        {
            view.x = 1f - edgeX;
            view.y = 0.5f;
        }
        else
        {
            /* 3) Inside horizontal range ¡æ keep X; clamp Y 2-98 %.       */
            const float edgeY = 0.02f;
            view.y = Mathf.Clamp(view.y, edgeY, 1f - edgeY);
        }

        /* -------------------------------------------------------------- */
        /* 4) viewport ¡æ screen point ¡æ local canvas point                */
        /* -------------------------------------------------------------- */
        Vector3 screenPos = _worldCam.ViewportToScreenPoint(view);

        TMP_Text txt = Instantiate(_popupPrefab, _canvas.transform);
        txt.text = $"+{amount:0}";
        RectTransform rt = txt.rectTransform;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform,
            screenPos,
            null,                // overlay canvas => cam == null
            out Vector2 localPos);

        rt.anchoredPosition = localPos;

        StartCoroutine(AnimatePopup(rt, txt.GetComponent<CanvasGroup>()));
    }

    private IEnumerator AnimatePopup(RectTransform rt, CanvasGroup cg)
    {
        float t = 0f;
        Vector2 start = rt.anchoredPosition;
        Vector2 end = start + Vector2.up * _riseDistance;

        while (t < _duration)
        {
            float ratio = t / _duration;
            rt.anchoredPosition = Vector2.Lerp(start, end, ratio);
            if (cg != null) cg.alpha = 1f - ratio;
            t += Time.unscaledDeltaTime;  // runs even when paused
            yield return null;
        }
        Destroy(rt.gameObject);
    }
}
