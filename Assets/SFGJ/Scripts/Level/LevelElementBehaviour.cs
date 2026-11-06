using UnityEngine;

namespace SFGJ.Level
{
    /// <summary>
    /// 关卡元素组件。由关卡编辑器直接挂到场景对象上，标记元素的类型、占据的格子与关联信息。
    /// 解密逻辑（开关开门、钥匙开门、压力板、触发器事件等）在此扩展。
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

        [Tooltip("备注 / 对话标题 / 提示文字等自定义内容")]
        public string note;

        /// <summary>玩家与元素交互（按键/点击）时调用。</summary>
        public virtual void Interact() { }

        /// <summary>元素被触发（开关、压力板、触发器）时调用。</summary>
        public virtual void OnTriggered() { }
    }
}
