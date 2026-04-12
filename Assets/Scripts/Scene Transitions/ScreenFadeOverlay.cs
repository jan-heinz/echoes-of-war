using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// runtime-created full-screen fade overlay that persists across scene loads
public class ScreenFadeOverlay : MonoBehaviour
{
    private static ScreenFadeOverlay instance;

    private Canvas overlayCanvas;
    private CanvasGroup overlayCanvasGroup;
    private bool fadeInOnNextSceneLoad;
    private float nextSceneFadeInDuration = 0.4f;

    public static ScreenFadeOverlay Instance => EnsureInstance();

    public static ScreenFadeOverlay EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        var overlayObject = new GameObject("ScreenFadeOverlay");
        instance = overlayObject.AddComponent<ScreenFadeOverlay>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildOverlay();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    public IEnumerator FadeToBlack(float durationSeconds)
    {
        yield return FadeToAlpha(1f, durationSeconds);
    }

    public IEnumerator FadeFromBlack(float durationSeconds)
    {
        yield return FadeToAlpha(0f, durationSeconds);
    }

    public void PrepareFadeInOnNextSceneLoad(float durationSeconds)
    {
        BuildOverlay();
        nextSceneFadeInDuration = Mathf.Max(0f, durationSeconds);
        fadeInOnNextSceneLoad = true;
        SetOverlayAlpha(1f);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        if (!fadeInOnNextSceneLoad)
        {
            return;
        }

        fadeInOnNextSceneLoad = false;
        StartCoroutine(FadeFromBlack(nextSceneFadeInDuration));
    }

    private IEnumerator FadeToAlpha(float targetAlpha, float durationSeconds)
    {
        BuildOverlay();

        if (durationSeconds <= 0f)
        {
            SetOverlayAlpha(targetAlpha);
            yield break;
        }

        var startAlpha = overlayCanvasGroup.alpha;
        var elapsed = 0f;

        while (elapsed < durationSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(elapsed / durationSeconds);
            SetOverlayAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        SetOverlayAlpha(targetAlpha);
    }

    private void BuildOverlay()
    {
        if (overlayCanvas != null)
        {
            return;
        }

        overlayCanvas = gameObject.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = short.MaxValue;

        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        overlayCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        var imageObject = new GameObject("Fade");
        imageObject.transform.SetParent(transform, false);

        var rectTransform = imageObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        var overlayImage = imageObject.AddComponent<Image>();
        overlayImage.color = Color.black;
        overlayImage.raycastTarget = true;

        SetOverlayAlpha(0f);
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (overlayCanvasGroup == null)
        {
            return;
        }

        var clampedAlpha = Mathf.Clamp01(alpha);
        overlayCanvasGroup.alpha = clampedAlpha;
        overlayCanvasGroup.blocksRaycasts = clampedAlpha > 0.001f;
        overlayCanvasGroup.interactable = clampedAlpha > 0.001f;
    }
}
