using UnityEngine;
using UnityEngine.Events;

namespace SFGJ.Level
{
    /// <summary>
    /// 通用计数器：记录数量，数量达到目标值时触发 <see cref="onReached"/> 事件。
    /// 可用于收集钥匙 / 碎片 / 推块到位次数等一切「凑满 N 个」的判定。
    /// </summary>
    public class Counter : MonoBehaviour
    {
        [Header("计数")]
        [Tooltip("当前数量")]
        public int count;

        [Tooltip("目标值（数量 ≥ 目标值时触发事件）")]
        public int target = 3;

        [Header("事件")]
        [Tooltip("数量达到目标值时触发")]
        public UnityEvent onReached;

        [Tooltip("触发过一次后不再重复触发（勾选则只触发一次）")]
        public bool once = true;

        private bool _triggered;

        /// <summary>当前数量。</summary>
        public int Count => count;

        /// <summary>是否已达到目标。</summary>
        public bool Reached => count >= target;

        /// <summary>加数（默认 +1，可传任意增量）。</summary>
        public void Add(int amount = 1)
        {
            count += amount;
            CheckReached();
        }

        /// <summary>直接设置数量并检查条件。</summary>
        public void SetCount(int value)
        {
            count = value;
            CheckReached();
        }

        /// <summary>重置计数与触发状态。</summary>
        public void Reset()
        {
            count = 0;
            _triggered = false;
        }

        private void CheckReached()
        {
            if (_triggered && once) return;
            if (count < target) return;

            _triggered = true;
            onReached?.Invoke();
        }
    }
}
