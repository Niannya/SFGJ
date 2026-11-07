using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SFGJ.Level
{
    /// <summary>
    /// 场景过渡器（单例）：把 NineMapScene / MapRingController 的淡入淡出与「黑屏停顿感」搬到通用传送流程。
    ///
    /// 节奏完全对齐 MapRingController.Transition：
    ///   冻结玩家 + blocksRaycasts 挡输入 → 淡出到全黑 → 全黑中执行瞬移
    ///   → Physics2D.SyncTransforms() → 停顿一帧（可再加 blackoutHold）→ 淡入 → 解冻。
    /// Fade 使用 Time.unscaledDeltaTime，暂停 / 慢动作下也照常播放。
    ///
    /// 开箱即用：场景里没有实例时 <see cref="GetOrCreate"/> 自动创建；
    /// 没有黑幕时自动创建一个全屏 CanvasGroup（与 NineMapSceneBuilder.AddFadeCanvas 同款）。
    /// 想调参数，就在场景里挂一个本组件再改 fadeSeconds / blackoutHold。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class SceneTransitionFader : MonoBehaviour
    {
        public static SceneTransitionFader Instance { get; private set; }

        [Header("淡入淡出")]
        [SerializeField, Min(0.01f)]
        [Tooltip("单次淡入 / 淡出时长（秒），与 NineMapScene 一致：0.4")]
        private float fadeSeconds = 0.4f;

        [SerializeField, Min(0f)]
        [Tooltip("全黑状态额外停顿（秒）——这就是那种「停顿感」；设 0 则只停一帧，完全还原原脚本")]
        private float blackoutHold = 0.12f;

        [SerializeField]
        [Tooltip("淡入淡出黑幕；留空则运行时自动创建（优先复用场景里的 BlackFadeOverlay）")]
        private CanvasGroup fadeOverlay;

        [SerializeField]
        [Tooltip("开场是否先全黑再淡入（对应 NineMapScene 的 InitialReveal）")]
        private bool revealOnStart;

        /// <summary>当前是否正在过渡（传送点用它防重入）。</summary>
        public bool IsTransitioning { get; private set; }

        public static bool IsBusy => Instance != null && Instance.IsTransitioning;

        // ---------------- 单例 / 自动创建 ----------------

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            EnsureOverlay();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>取得（必要时自动创建）过渡器，任何场景都能直接调用。</summary>
        public static SceneTransitionFader GetOrCreate()
        {
            if (Instance != null) return Instance;

            var found = FindObjectOfType<SceneTransitionFader>();
            if (found != null)
            {
                Instance = found;
                found.EnsureOverlay();
                return found;
            }

            var go = new GameObject("SceneTransitionFader");
            return go.AddComponent<SceneTransitionFader>();
        }

        private void Start()
        {
            if (revealOnStart) StartCoroutine(InitialReveal());
        }

        /// <summary>开场：从全黑淡入（与 MapRingController.InitialReveal 一致）。</summary>
        private IEnumerator InitialReveal()
        {
            if (fadeOverlay == null) yield break;
            fadeOverlay.alpha = 1f;
            fadeOverlay.blocksRaycasts = true;
            yield return Fade(0f);
            fadeOverlay.blocksRaycasts = false;
        }

        // ---------------- 过渡 ----------------

        /// <summary>
        /// 播放一次带淡入淡出 + 停顿的过渡。
        /// </summary>
        /// <param name="onCovered">全黑时执行（在这里做瞬移 / 换场景，玩家看不见）。</param>
        /// <param name="onComplete">淡入结束后执行（可选）。</param>
        public void PlayTransition(Action onCovered, Action onComplete = null)
        {
            if (IsTransitioning) return;
            StartCoroutine(Transition(onCovered, onComplete));
        }

        private IEnumerator Transition(Action onCovered, Action onComplete)
        {
            IsTransitioning = true;

            var player = FindObjectOfType<PlayerController>();
            if (player != null) player.SetCanMove(false);
            if (fadeOverlay != null) fadeOverlay.blocksRaycasts = true;

            // 1) 淡出到全黑
            yield return Fade(1f);

            // 2) 全黑里完成瞬移，同步物理，再停顿——「停顿感」就在这里
            onCovered?.Invoke();
            Physics2D.SyncTransforms();
            yield return null;
            if (blackoutHold > 0f)
                yield return new WaitForSecondsRealtime(blackoutHold);

            // 3) 淡入恢复
            yield return Fade(0f);
            if (fadeOverlay != null) fadeOverlay.blocksRaycasts = false;
            if (player != null) player.SetCanMove(true);

            IsTransitioning = false;
            onComplete?.Invoke();
        }

        /// <summary>与 MapRingController.Fade 一致：unscaledDeltaTime 从当前 alpha 插值到 target。</summary>
        private IEnumerator Fade(float target)
        {
            if (fadeOverlay == null) yield break;

            float initial = fadeOverlay.alpha;
            float elapsed = 0f;
            while (elapsed < fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeOverlay.alpha = Mathf.Lerp(initial, target, Mathf.Clamp01(elapsed / fadeSeconds));
                yield return null;
            }
            fadeOverlay.alpha = target;
        }

        // ---------------- 自动黑幕 ----------------

        private void EnsureOverlay()
        {
            if (fadeOverlay != null) return;

            // 优先复用场景里已有的黑幕（例如 NineMapScene 的 BlackFadeOverlay）
            var existing = GameObject.Find("BlackFadeOverlay");
            if (existing != null)
            {
                fadeOverlay = existing.GetComponent<CanvasGroup>();
                if (fadeOverlay != null) return;
            }

            fadeOverlay = CreateOverlay();
        }

        private static CanvasGroup CreateOverlay()
        {
            var canvasObject = new GameObject("SceneFadeCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var overlay = new GameObject("BlackFadeOverlay", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            overlay.transform.SetParent(canvasObject.transform, false);
            var rect = overlay.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = overlay.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = true;

            var group = overlay.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            return group;
        }
    }
}
