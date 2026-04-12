using UnityEngine;

// simple CTA pulse for UI buttons
public class UiPulse : MonoBehaviour
{
    [SerializeField] private RectTransform target;
    [SerializeField] [Min(1f)] private float maxScaleMultiplier = 1.06f;
    [SerializeField] [Min(0f)] private float pulsesPerSecond = 1.4f;

    private Vector3 baseScale = Vector3.one;

    private void Awake()
    {
        if (target == null)
        {
            target = transform as RectTransform;
        }

        if (target != null)
        {
            baseScale = target.localScale;
        }
    }

    private void OnEnable()
    {
        if (target == null)
        {
            target = transform as RectTransform;
        }

        if (target == null)
        {
            enabled = false;
            return;
        }

        baseScale = target.localScale;
        target.localScale = baseScale;
    }

    private void OnDisable()
    {
        if (target != null)
        {
            target.localScale = baseScale;
        }
    }

    private void Update()
    {
        if (target == null)
        {
            return;
        }

        var cycle = Mathf.Sin(Time.unscaledTime * pulsesPerSecond * Mathf.PI * 2f);
        var t = (cycle + 1f) * 0.5f;
        var scaleMultiplier = Mathf.Lerp(1f, maxScaleMultiplier, t);
        target.localScale = baseScale * scaleMultiplier;
    }
}
