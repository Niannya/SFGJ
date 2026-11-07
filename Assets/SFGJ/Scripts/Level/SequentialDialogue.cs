using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using PixelCrushers.DialogueSystem;

namespace SFGJ.Level
{
    /// <summary>
    /// 顺序对话组件（例如医生：第一次交互说第一段，再次交互说下一段）。
    /// 按 <see cref="conversations"/> 顺序逐次播放，播放完最后一段后触发 <see cref="onFinished"/>。
    /// </summary>
    public class SequentialDialogue : MonoBehaviour, IInteractable
    {
        [Header("对话")]
        [Tooltip("按顺序播放的对话标题（每次交互播下一段）")]
        public List<string> conversations = new List<string>();

        [Header("结束")]
        [Tooltip("最后一段对话结束后触发（可选）")]
        public UnityEvent onFinished;

        private int _index;

        public void Interact(InteractionUI interactionUI)
        {
            if (conversations == null || conversations.Count == 0) return;

            int idx = Mathf.Min(_index, conversations.Count - 1);
            string title = conversations[idx];
            bool isLast = idx >= conversations.Count - 1;

            if (_index < conversations.Count)
                _index++;

            StartDialogue(title, isLast ? onFinished : null);
        }

        private UnityEvent _pending;

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
