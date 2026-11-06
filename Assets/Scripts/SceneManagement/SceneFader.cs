using System.Collections;
using UnityEngine;

public class SceneFader : MonoBehaviour
{
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float fadeDuration = 0.4f;
    private Coroutine fadeInCoroutine;

    private void Awake()
    {
        fadeOverlay.alpha = 1f;
    }

    private void Start()
    {
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
