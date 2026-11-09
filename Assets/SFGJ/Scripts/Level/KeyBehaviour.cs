using UnityEngine;
using UnityEngine.Events;
using PixelCrushers.DialogueSystem;

namespace SFGJ.Level
{
    /// <summary>
    /// 钥匙 / 碎片组件（挂在 Key 元素上）。
    /// 玩家交互 → 收集（触发 <see cref="onCollected"/>，钥匙消失）；若配置了对话，则先播放对话、对话结束后再收集。
    /// 注意：不内置任何计数逻辑，具体行为（例如调用 BedroomController.IncreaseKeyCount）由你在 Inspector 的 onCollected 里配置，
    /// 这样不同场景的钥匙可以各接各的控制器，互不影响。
    /// </summary>
    public class KeyBehaviour : MonoBehaviour, IInteractable
    {
        [Header("对话（可选）")]
        [Tooltip("对话标题（留空则收集时不对话）")]
        public string conversationTitle;

        [Header("收集")]
        [Tooltip("收集时触发的事件（在这里配置具体行为，例如 BedroomController.IncreaseKeyCount）")]
        public UnityEvent onCollected;

        [Tooltip("收集后是否让钥匙消失")]
        public bool destroyOnCollect = true;

        public void Interact(InteractionUI interactionUI)
        {
            if (string.IsNullOrWhiteSpace(conversationTitle))
            {
                Collect();
            }
            else
            {
                StartDialogue();
            }
        }

        private void Collect()
        {
            onCollected?.Invoke();

            if (destroyOnCollect)
                gameObject.SetActive(false);
        }

        private void StartDialogue()
        {
            if (!DialogueManager.hasInstance || DialogueManager.isConversationActive) return;

            var player = FindObjectOfType<PlayerController>();
            if (player != null) player.SetCanMove(false);

            DialogueManager.StartConversation(
                conversationTitle,
                player != null ? player.transform : transform,
                transform);
        }

        // 对话结束（Pixel Crushers 隐式消息）→ 收集
        private void OnConversationEnd(Transform actor)
        {
            var player = FindObjectOfType<PlayerController>();
            if (player != null) player.SetCanMove(true);

            Collect();
        }
    }
}
