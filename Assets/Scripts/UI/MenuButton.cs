using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 开始界面的菜单按钮。
/// 悬浮：图片放大一圈 + 上下轻微浮动；离开：恢复。
/// 点击：淡入黑屏 → 切换到目标场景。
/// </summary>
public class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("悬浮效果")]
    [Tooltip("要放大/浮动的图片；留空则自动找按钮下的子 Image")]
    [SerializeField] private RectTransform targetImage;

    [Tooltip("悬浮时放大倍数（一圈）")]
    [SerializeField] private float hoverScale = 1.15f;

    [Tooltip("悬浮时上下浮动幅度（像素）")]
    [SerializeField] private float floatAmplitude = 8f;

    [Tooltip("浮动速度")]
    [SerializeField] private float floatSpeed = 3f;

    [Tooltip("放大/缩小的过渡速度（越大越跟手）")]
    [SerializeField] private float scaleLerp = 12f;

    [Header("点击切换场景")]
    [Tooltip("要切换到的场景名")]
    [SerializeField] private string sceneName;

    [Tooltip("淡入黑屏时长（秒）")]
    [SerializeField] private float fadeDuration = 0.4f;

    private bool _hovering;
    private bool _clicked;
    private Vector2 _basePos;
    private Vector3 _baseScale;

    private void Awake()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<RectTransform>();
            // 优先用按钮里的图片（子 Image）
            var childImg = GetComponentInChildren<Image>();
            if (childImg != null && childImg.transform != transform)
                targetImage = childImg.rectTransform;
        }
        _basePos = targetImage.anchoredPosition;
        _baseScale = targetImage.localScale;
    }

    private void Update()
    {
        if (_clicked) return;

        // 平滑放大/缩小
        float target = _hovering ? hoverScale : 1f;
        float cur = targetImage.localScale.x;
        float s = Mathf.Lerp(cur, target, scaleLerp * Time.unscaledDeltaTime);
        targetImage.localScale = new Vector3(s, s, 1f);

        // 悬浮时上下浮动
        if (_hovering)
        {
            float y = _basePos.y + Mathf.Sin(Time.unscaledTime * floatSpeed) * floatAmplitude;
            targetImage.anchoredPosition = new Vector2(_basePos.x, y);
        }
        else
        {
            targetImage.anchoredPosition = _basePos;
        }
    }

    public void OnPointerEnter(PointerEventData eventData) => _hovering = true;
    public void OnPointerExit(PointerEventData eventData) => _hovering = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_clicked) return;
        _clicked = true;
        StartCoroutine(LoadSceneWithFade());
    }

    private IEnumerator LoadSceneWithFade()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[菜单按钮] 未配置目标场景名。", this);
            yield break;
        }

        // 创建跨场景黑幕
        var fadeGO = SceneFadeOverlay.Create();
        var group = fadeGO.GetComponent<CanvasGroup>();
        var overlay = fadeGO.GetComponent<SceneFadeOverlay>();
        overlay.Init(fadeDuration);

        // 淡入黑屏
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        group.alpha = 1f;

        // 切场景（黑幕跨场景保留，场景加载后由 SceneFadeOverlay 自动淡出）
        SceneManager.LoadScene(sceneName);
    }
}
