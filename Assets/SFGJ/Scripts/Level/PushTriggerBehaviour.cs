using UnityEngine;
using UnityEngine.Events;

namespace SFGJ.Level
{
    /// <summary>
    /// 推块触发器组件（挂在 Trigger 元素上，专门用来检测推块）。
    /// 只检测「推块」是否进入触发区域；推块进入后触发 <see cref="onPushBlockEntered"/> /
    /// 退出后触发 <see cref="onPushBlockExited"/>，并且可选播一次对话。
    /// 玩家路过不会触发（区别于 TriggerBehaviour）。
    /// </summary>
    public class PushTriggerBehaviour : MonoBehaviour
    {
        [Header("事件")]
        [Tooltip("推块进入触发区时触发")]
        public UnityEvent onPushBlockEntered;

        [Tooltip("推块离开触发区时触发（可选）")]
        public UnityEvent onPushBlockExited;

        [Header("对话（可选）")]
        [Tooltip("推块首次进入时触发的对话标题；留空则不播对话")]
        public string conversationTitle;

        [Tooltip("推块进入事件触发过一次后，要不要保持不再触发")]
        public bool oneShot = true;

        private bool _played;
        private bool _inside;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PushBlockBehaviour>() == null) return;
            if (_inside) return;
            _inside = true;

            // 事件（可绑定 MazeController.SetFlag / 开门 等）
            if (!oneShot || !_played)
            {
                onPushBlockEntered?.Invoke();
            }

            // 对话（只播一次）
            if (!_played && !string.IsNullOrWhiteSpace(conversationTitle))
            {
                _played = true;
                PlayDialogue();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<PushBlockBehaviour>() == null) return;
            if (!_inside) return;
            _inside = false;
            onPushBlockExited?.Invoke();
        }

        /// <summary>重置触发器状态（让已经触发过的触发器能再次触发），配合推块重置使用。</summary>
        public void ResetState()
        {
            _played = false;
            _inside = false;
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
    }
}
