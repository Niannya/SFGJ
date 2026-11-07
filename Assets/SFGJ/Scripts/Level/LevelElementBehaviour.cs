using UnityEngine;

namespace SFGJ.Level
{
    /// <summary>
    /// 关卡元素组件（关卡即场景：直接挂在场景对象上）。
    /// 只保存元素的基础数据（类型 / 格子 / 关联），功能逻辑按类型挂独立组件：
    ///   - 传送点 Portal → <see cref="PortalBehaviour"/>
    ///   - 钥匙 Key     → <see cref="KeyBehaviour"/>
    ///   - NPC          → <see cref="NPCBehaviour"/>
    ///   - 触发器 Trigger → <see cref="TriggerBehaviour"/>
    /// </summary>
    public class LevelElementBehaviour : MonoBehaviour
    {
        [Tooltip("元素类型")]
        public LevelElementType type;

        [Tooltip("占据区域左下角的格子坐标")]
        public Vector2Int gridPos;

        [Tooltip("占据的格子数（>1 表示大物体/大区域）")]
        public Vector2Int gridSize = Vector2Int.one;

        [Tooltip("元素唯一 ID，用于开关/门/钥匙之间的关联")]
        public string elementId;

        [Tooltip("关联目标 ID（例如门对应要用的钥匙、开关要打开的门）")]
        public string linkId;

        [Tooltip("备注 / 提示文字等自定义内容")]
        public string note;

        private void Awake()
        {
            SyncColliderTrigger();
        }

        /// <summary>根据类型同步碰撞体是否可穿过（Key/NPC 等实心，Trigger 等可穿过；Portal 交给 PortalBehaviour 控制）。</summary>
        private void SyncColliderTrigger()
        {
            var col = GetComponent<Collider2D>();
            if (col == null) return;
            if (type == LevelElementType.Portal) return; // Portal 的碰撞器由 PortalBehaviour 按传送开关控制
            col.isTrigger = !LevelPalette.IsSolid(type);
        }

        /// <summary>玩家与元素交互（按键/点击）时调用。</summary>
        public virtual void Interact() { }

        /// <summary>元素被触发（开关、压力板、触发器）时调用。</summary>
        public virtual void OnTriggered() { }
    }
}
