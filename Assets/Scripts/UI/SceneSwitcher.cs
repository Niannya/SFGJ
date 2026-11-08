using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景切换控制器：调用 SwitchScene 切换到其它场景，切换前先淡入黑屏
/// （复用 SceneFadeOverlay：淡入 → 切场景 → 新场景加载后自动淡出）。
/// </summary>
public class SceneSwitcher : MonoBehaviour
{
    [Tooltip("要切换到的场景名")]
    [SerializeField] private string targetSceneName;

    [Tooltip("淡入黑屏时长（秒）")]
    [SerializeField] private float fadeDuration = 0.5f;

    /// <summary>切换到 targetSceneName（带淡入黑屏）。</summary>
    public void SwitchScene()
    {
        SwitchScene(targetSceneName);
    }

    /// <summary>切换到指定场景（带淡入黑屏）。</summary>
    public void SwitchScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[场景切换] 未指定目标场景名。", this);
            return;
        }
        StartCoroutine(Transition(sceneName));
    }

    private IEnumerator Transition(string sceneName)
    {
        // 跨场景黑幕：淡入后切场景，新场景加载后由 SceneFadeOverlay 自动淡出
        var fadeGO = SceneFadeOverlay.Create();
        var group = fadeGO.GetComponent<CanvasGroup>();
        fadeGO.GetComponent<SceneFadeOverlay>().Init(fadeDuration);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        group.alpha = 1f;

        SceneManager.LoadScene(sceneName);
    }
}
