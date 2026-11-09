using UnityEngine;

namespace SFGJ.Level
{
    /// <summary>
    /// 卧室场景控制器（单例）。
    /// 管理钥匙收集计数；当收集齐所有钥匙后，把 NPC 哥哥的对话切换为「哥哥交予」。
    /// </summary>
    public class BedroomController : MonoBehaviour
    {
        public static BedroomController Instance { get; private set; }

        [Header("钥匙收集")]
        [Tooltip("当前已收集的钥匙数量")]
        public int keyCount;

        [Tooltip("需要收集的钥匙总数")]
        public int totalKeys = 3;

        [Header("哥哥 NPC")]
        [Tooltip("哥哥的 NPC 组件（收集齐后要切换对话的目标）")]
        public NPCBehaviour brotherNPC;

        [Tooltip("收集齐后哥哥要切换成的对话标题")]
        public string giveConversation = "哥哥交予";

        private void Awake()
        {
            Instance = this;
        }

        /// <summary>增加一个钥匙计数；收集齐后把哥哥的对话切换为交予对话。</summary>
        public void IncreaseKeyCount()
        {
            keyCount++;

            if (keyCount >= totalKeys)
            {
                SwitchBrotherDialogue();
            }
        }

        private void SwitchBrotherDialogue()
        {
            if (brotherNPC != null && !string.IsNullOrWhiteSpace(giveConversation))
            {
                brotherNPC.conversationTitle = giveConversation;
            }
        }
    }
}
