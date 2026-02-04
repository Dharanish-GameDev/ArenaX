using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class UIButtonElasticBounce : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private RectTransform target;

    [Header("Touch Zoom (Press)")]
    [SerializeField] private float touchScale = 0.96f;
    [SerializeField] private float touchTime = 0.06f;

    [Header("Click Elastic Bounce")]
    [SerializeField] private float pressScale = 0.92f;
    [SerializeField] private float popScale = 1.08f;
    [SerializeField] private float pressTime = 0.05f;
    [SerializeField] private float popTime = 0.12f;
    [SerializeField] private float settleTime = 0.10f;

    private Vector3 baseScale;
    private Coroutine routine;
    private Button btn;
    private bool isPointerDown = false;

    private void Awake()
    {
        if (target == null) target = transform as RectTransform;
        baseScale = target.localScale;

        btn = GetComponent<Button>();
        btn.onClick.AddListener(PlayElasticBounce);
    }

    // ✅ Touch / press down (little zoom)
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!btn.interactable) return;

        isPointerDown = true;
        StartScale(baseScale * touchScale, touchTime, EaseOutCubic);
    }

    // ✅ Release back to normal
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!btn.interactable) return;

        isPointerDown = false;
        StartScale(baseScale, touchTime, EaseOutCubic);
    }

    // ✅ If finger/mouse leaves button, reset
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!btn.interactable) return;

        isPointerDown = false;
        StartScale(baseScale, touchTime, EaseOutCubic);
    }

    // ✅ Click bounce
    public void PlayElasticBounce()
    {
        if (!btn.interactable) return;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ElasticBounce());
    }

    private IEnumerator ElasticBounce()
    {
        yield return ScaleTo(baseScale * pressScale, pressTime, EaseOutCubic);
        yield return ScaleTo(baseScale * popScale, popTime, EaseOutElastic);
        yield return ScaleTo(baseScale, settleTime, EaseOutCubic);

        // if still holding, keep touch-scale
        if (isPointerDown)
            target.localScale = baseScale * touchScale;

        routine = null;
    }

    private void StartScale(Vector3 targetScale, float time, System.Func<float, float> ease)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ScaleTo(targetScale, time, ease));
    }

    private IEnumerator ScaleTo(Vector3 targetScale, float time, System.Func<float, float> ease)
    {
        Vector3 start = target.localScale;
        float t = 0f;

        if (time <= 0f)
        {
            target.localScale = targetScale;
            yield break;
        }

        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / time);
            p = ease(p);

            target.localScale = Vector3.LerpUnclamped(start, targetScale, p);
            yield return null;
        }

        target.localScale = targetScale;
    }

    private float EaseOutCubic(float x)
    {
        return 1f - Mathf.Pow(1f - x, 3f);
    }

    private float EaseOutElastic(float x)
    {
        if (x == 0f) return 0f;
        if (x == 1f) return 1f;

        float c4 = (2f * Mathf.PI) / 3f;
        return Mathf.Pow(2f, -10f * x) * Mathf.Sin((x * 10f - 0.75f) * c4) + 1f;
    }
}
