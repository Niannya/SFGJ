using UnityEngine;
using UnityEngine.Events;
using PixelCrushers.DialogueSystem;

namespace SFGJ.Level
{
    /// <summary>
    /// 条件对话组件（例如剧情里的哥哥）。
    /// 玩家交互时根据收集到的钥匙数量选择对话：
    ///   - 未集齐：播放 <see cref="noKeyConversation"/>。
    ///   - 集齐（≥ <see cref="requiredKeys"/>）：播放 <see cref="giveConversation"/>，结束后触发 <see cref="onGiveFinished"/>（用于开门等）。
    /// </summary>
    public class ConditionalDialogue : MonoBehaviour, IInteractable
    {
        [Header("条件")]
        [Tooltip("需要的钥匙数量")]
        public int requiredKeys = 3;

        [Header("对话")]
        [Tooltip("未集齐时的对话标题")]
        public string noKeyConversation;

        [Tooltip("集齐后的交予对话标题")]
        public string giveConversation;

        [Header("交予后")]
        [Tooltip("交予对话结束后触发（例如打开门）")]
        public UnityEvent onGiveFinished;

        private UnityEvent _pending;

        public void Interact(InteractionUI interactionUI)
        {
            int keys = MazeController.Instance != null ? MazeController.Instance.collectedKeys : 0;
            if (keys >= requiredKeys)
                StartDialogue(giveConversation, onGiveFinished);
            else
                StartDialogue(noKeyConversation, null);
        }

        private void StartDialogue(string title, UnityEvent onFinished)
        {
            if (string.IsNullOrWhiteSpace(title)) return;
            if (!DialogueManager.hasInstance || DialogueManager.isConversationActive) return;

            var player = FindObjectOfType<PlayerController>();
            if (player != null) player.SetCanMove(false);

            _pending = onFinished;
            DialogueManager.StartConversation(
                title,
                player != null ? player.transform : transform,
                transform);
        }

        // 对话结束（Pixel Crushers 隐式消息）
        private void OnConversationEnd(Transform actor)
        {
            var player = FindObjectOfType<PlayerController>();
            if (player != null) player.SetCanMove(true);

            if (_pending != null)
            {
                var ev = _pending;
                _pending = null;
                ev.Invoke();
            }
        }
    }
}
