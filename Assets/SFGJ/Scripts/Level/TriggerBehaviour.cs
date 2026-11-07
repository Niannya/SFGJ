using UnityEngine;
using UnityEngine.Events;

namespace SFGJ.Level
{
    /// <summary>
    /// 触发器组件（挂在 Trigger 元素上）。
    /// 玩家进入触发区域 → 自动触发 <see cref="onTriggered"/>。
    /// </summary>
    public class TriggerBehaviour : MonoBehaviour
    {
        [Header("事件")]
        [Tooltip("玩家进入时触发")]
        public UnityEvent onTriggered;

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player == null) return;

            onTriggered?.Invoke();
        }
    }
}
