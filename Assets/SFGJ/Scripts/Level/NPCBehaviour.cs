using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace SFGJ.Level
{
    /// <summary>
    /// NPC 组件（挂在 NPC 元素上）。
    /// 玩家交互 → 只播放对话。
    /// </summary>
    public class NPCBehaviour : MonoBehaviour, IInteractable
    {
        [Header("对话")]
        [Tooltip("对话标题（对应 Dialogue 数据库里的 Conversation）")]
        public string conversationTitle;

        public void Interact(InteractionUI interactionUI)
        {
            StartDialogue();
        }

        private void StartDialogue()
        {
            if (string.IsNullOrWhiteSpace(conversationTitle)) return;
            if (!DialogueManager.hasInstance || DialogueManager.isConversationActive) return;

            var player = FindObjectOfType<PlayerController>();
            if (player != null) player.SetCanMove(false);

            DialogueManager.StartConversation(
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
    }
}
