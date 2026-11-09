using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace SFGJ.Level
{
    /// <summary>
    /// 延迟动作：延迟指定秒数后，执行一次事件并播放淡入淡出过渡。
    /// 事件在「淡入淡出的黑屏期间」执行（适合传送 / 切场景等需要遮掩的瞬间动作）。
    /// </summary>
    public class DelayedAction : MonoBehaviour
    {
        [Header("延迟")]
        [Tooltip("延迟秒数")]
        public float delay = 2f;

        [Tooltip("启动时自动开始延迟；取消勾选则用 Trigger() 手动触发")]
        public bool autoStart = true;

        [Header("事件")]
        [Tooltip("延迟结束后执行的事件（在淡入淡出黑屏期间执行）")]
        public UnityEvent onFinished;

        [Header("淡入淡出")]
        [Tooltip("是否播放淡入淡出过渡（黑屏停顿 + 淡入淡出）")]
        public bool playTransition = true;

        private void Start()
        {
            if (autoStart) Trigger();
        }

        /// <summary>手动触发延迟。</summary>
        public void Trigger()
        {
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            if (playTransition)
            {
                SceneTransitionFader.GetOrCreate().PlayTransition(() => onFinished?.Invoke());
            }
            else
            {
                onFinished?.Invoke();
            }
        }
    }
}
