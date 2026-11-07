using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace SFGJ
{
    /// <summary>
    /// 开局自动开始一段对话，用于验证对话 UI 正常显示。
    /// 对话内容在 Dialogue Database 的对应对话里编辑（默认为 "Opening"）。
    /// </summary>
    public class OpeningDialogue : MonoBehaviour
    {
        [Tooltip("要开始的对话标题")]
        public string conversationTitle = "Opening";

        [Tooltip("延迟多少秒开始（给 Dialogue Manager 初始化留时间）")]
        public float delay = 0.5f;

        private void Start()
        {
            if (delay > 0f)
                Invoke(nameof(StartOpening), delay);
            else
                StartOpening();
        }

        private void StartOpening()
        {
            if (string.IsNullOrEmpty(conversationTitle)) return;
            if (DialogueManager.IsConversationActive) return;
            DialogueManager.StartConversation(conversationTitle);
        }
    }
}
