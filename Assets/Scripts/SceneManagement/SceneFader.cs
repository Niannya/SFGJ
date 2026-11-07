using System.Collections;
using UnityEngine;

public class SceneFader : MonoBehaviour
{
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float fadeDuration = 0.4f;
    private Coroutine fadeInCoroutine;

    public bool IsFadingIn => fadeInCoroutine != null;

    // Allows an opening scene to create its overlay before this component starts.
    public void Configure(CanvasGroup overlay)
    {
        fadeOverlay = overlay;
        if (fadeOverlay != null) fadeOverlay.alpha = 1f;
    }

    private void Awake()
    {
        if (fadeOverlay != null) fadeOverlay.alpha = 1f;
    }

    private void Start()
    {
        if (fadeOverlay == null)
        {
            Debug.LogError("SceneFader needs a fade overlay.", this);
            enabled = false;
            return;
        }
        fadeInCoroutine = StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        yield return null;
        float startTime = Time.realtimeSinceStartup;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed = Time.realtimeSinceStartup - startTime;
            fadeOverlay.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }
        fadeOverlay.alpha = 0f;
        fadeInCoroutine = null;
    }

    public IEnumerator FadeOut()
    {
        if (fadeOverlay == null)
        {
            Debug.LogError("SceneFader needs a fade overlay.", this);
            yield break;
        }
        if (fadeInCoroutine != null)
        {
            StopCoroutine(fadeInCoroutine);
            fadeInCoroutine = null;
        }

        float startTime = Time.realtimeSinceStartup;
        float startAlpha = fadeOverlay.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed = Time.realtimeSinceStartup - startTime;
            fadeOverlay.alpha =
                Mathf.Lerp(startAlpha, 1f, elapsed / fadeDuration);

            yield return null;
        }

        fadeOverlay.alpha = 1f;
    }
}
