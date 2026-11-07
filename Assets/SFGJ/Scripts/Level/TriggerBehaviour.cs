using UnityEngine;
using UnityEngine.Events;

namespace SFGJ.Level
{
    /// <summary>
    /// 触发器组件（挂在 Trigger 元素上）。
    /// 玩家进入触发区域 → 触发 <see cref="onTriggered"/> 事件，并可同时播放一段对话。
    /// 带开启开关：关闭时（<see cref="isEnabled"/>=false）玩家进入不触发，可用 <see cref="Enable"/> 等方法启动。
    /// </summary>
    public class TriggerBehaviour : MonoBehaviour
    {
        [Header("开关")]
        [SerializeField]
        [Tooltip("触发器是否开启；关闭时玩家进入不触发")]
        private bool isEnabled = true;

        [Header("事件")]
        [Tooltip("玩家进入时触发")]
        public UnityEvent onTriggered;

        [Header("对话（可选）")]
        [Tooltip("玩家进入时要播放的对话标题；留空则不播对话")]
        public string conversationTitle;

        [Tooltip("对话是否只播一次（事件始终每次触发）")]
        public bool oneShot = true;

        private bool _played;

        /// <summary>触发器是否开启。</summary>
        public bool IsEnabled => isEnabled;

        /// <summary>启动（开启）触发器。</summary>
        public void Enable() => isEnabled = true;

        /// <summary>关闭触发器。</summary>
        public void Disable() => isEnabled = false;

        /// <summary>设置触发器是否开启。</summary>
        public void SetEnabled(bool enabled) => isEnabled = enabled;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isEnabled) return;

            var player = other.GetComponent<PlayerController>();
            if (player == null) return;

            onTriggered?.Invoke();

            if (!string.IsNullOrWhiteSpace(conversationTitle) && (!oneShot || !_played))
            {
                _played = true;
                PlayDialogue();
            }
        }

        private void PlayDialogue()
        {
            if (!PixelCrushers.DialogueSystem.DialogueManager.hasInstance) return;
            if (PixelCrushers.DialogueSystem.DialogueManager.isConversationActive) return;

            var player = FindObjectOfType<PlayerController>();
            if (player != null) player.SetCanMove(false);

            PixelCrushers.DialogueSystem.DialogueManager.StartConversation(
                conversationTitle,
                player != null ? player.transform : transform,
                transform);
        }

        // 对话结束（Pixel Crushers 隐式消息）
        private void OnConversationEnd(Transform actor)
        {
            var player = FindObjectOfType<PlayerController>();
            if (player != null) player.SetCanMove(true);
        }

        /// <summary>重置「只播一次」的状态，让对话能再次播放。</summary>
        public void ResetState()
        {
            _played = false;
        }
    }
}
