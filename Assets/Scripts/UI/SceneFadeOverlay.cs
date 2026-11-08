using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 跨场景的淡入黑屏遮罩（DontDestroyOnLoad）。
/// 由 MenuButton 点击时创建：淡入到全黑 → 切场景 → 新场景加载后自动淡出并销毁自己。
/// </summary>
public class SceneFadeOverlay : MonoBehaviour
{
    private CanvasGroup _group;
    private float _fadeDuration;

    /// <summary>创建一个全屏黑幕（Canvas + 全屏黑色 Image + CanvasGroup），并挂本组件。</summary>
    public static GameObject Create()
    {
        var go = new GameObject("SceneFadeOverlay");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        go.AddComponent<CanvasScaler>();
        var group = go.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = true;
        group.blocksRaycasts = true;

        var black = new GameObject("Black", typeof(RectTransform), typeof(Image));
        black.transform.SetParent(go.transform, false);
        var rect = black.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        black.GetComponent<Image>().color = Color.black;

        go.AddComponent<SceneFadeOverlay>();
        Object.DontDestroyOnLoad(go);
        return go;
    }

    public void Init(float fadeDuration)
    {
        _fadeDuration = fadeDuration;
        _group = GetComponent<CanvasGroup>();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        yield return null; // 等新场景一帧

        float from = _group.alpha;
        float elapsed = 0f;
        while (elapsed < _fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _group.alpha = Mathf.Lerp(from, 0f, Mathf.Clamp01(elapsed / _fadeDuration));
            yield return null;
        }
        _group.alpha = 0f;
        Destroy(gameObject);
    }
}
