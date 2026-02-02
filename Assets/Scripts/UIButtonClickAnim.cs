using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonElasticBounce : MonoBehaviour
{
    [SerializeField] private RectTransform target;

    [Header("Scales")]
    [SerializeField] private float pressScale = 0.92f;     // goes smaller first
    [SerializeField] private float popScale = 1.08f;       // elastic overshoot

    [Header("Timing")]
    [SerializeField] private float pressTime = 0.06f;
    [SerializeField] private float popTime = 0.12f;
    [SerializeField] private float settleTime = 0.10f;

    private Vector3 baseScale;
    private Coroutine routine;

    private void Awake()
    {
        if (target == null) target = transform as RectTransform;
        baseScale = target.localScale;

        GetComponent<Button>().onClick.AddListener(PlayElasticBounce);
    }

    public void PlayElasticBounce()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ElasticBounce());
    }

    private IEnumerator ElasticBounce()
    {
        // 1) Press in (smooth)
        yield return ScaleTo(baseScale * pressScale, pressTime, EaseOutCubic);

        // 2) Pop out (elastic)
        yield return ScaleTo(baseScale * popScale, popTime, EaseOutElastic);

        // 3) Settle back (smooth)
        yield return ScaleTo(baseScale, settleTime, EaseOutCubic);

        routine = null;
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

    // Smooth + soft
    private float EaseOutCubic(float x)
    {
        return 1f - Mathf.Pow(1f - x, 3f);
    }

    // Soft elastic bounce (not too aggressive)
    private float EaseOutElastic(float x)
    {
        if (x == 0f) return 0f;
        if (x == 1f) return 1f;

        float c4 = (2f * Mathf.PI) / 3f;
        return Mathf.Pow(2f, -10f * x) * Mathf.Sin((x * 10f - 0.75f) * c4) + 1f;
    }
}
