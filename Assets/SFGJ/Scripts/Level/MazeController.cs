using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SFGJ.Level
{
    /// <summary>
    /// 迷宫控制器（单例）。挂在迷宫场景某个常驻对象上，
    /// 统一记录玩家在迷宫中的状态变化，并处理开关门逻辑。
    ///
    /// 用法：
    ///   1. 在 Inspector 里给「门」配置 <see cref="doorTarget"/>（门开启时要失活的物体）。
    ///   2. 触发某个机关/物体时，调用 <see cref="SetFlag"/> / <see cref="ToggleFlag"/> 修改状态标记。
    ///   3. 满足开门条件时调用 <see cref="OpenDoor"/>，门关联的物体会被 SetActive(false)。
    /// </summary>
    public class MazeController : MonoBehaviour
    {
        /// <summary>全局单例，供迷宫元素（开关/门/触发器）快速访问。</summary>
        public static MazeController Instance { get; private set; }

        [Header("钥匙 / 物品")]
        [Tooltip("是否已获得钥匙")]
        public bool hasKey;

        [Tooltip("是否已获得物品")]
        public bool hasItem;

        [Header("钥匙收集")]
        [Tooltip("当前已收集的钥匙数量")]
        public int collectedKeys;

        [Tooltip("需要收集的钥匙总数（收集齐时触发 onAllKeysCollected）")]
        public int totalKeys = 3;

        [Tooltip("收集齐所有钥匙时触发")]
        public UnityEvent onAllKeysCollected;

        [Header("状态标记列表")]
        [Tooltip("通用状态标记，记录玩家触发了哪些物体（开关、机关等），通过索引读写")]
        public List<bool> flags = new List<bool>();

        [Header("门")]
        [Tooltip("门是否已开启")]
        public bool isDoorOpen;

        [Tooltip("门开启时要失活的物体（例如挡住通道的墙 / 障碍物）")]
        public GameObject doorTarget;

        private void Awake()
        {
            Instance = this;
        }

        // ---------------- 钥匙 / 物品 ----------------

        /// <summary>设置是否持有钥匙。</summary>
        public void SetKey(bool value) => hasKey = value;

        /// <summary>设置是否持有物品。</summary>
        public void SetItem(bool value) => hasItem = value;

        // ---------------- 钥匙收集 ----------------

        /// <summary>是否已收集齐所有钥匙。</summary>
        public bool HasAllKeys => collectedKeys >= totalKeys;

        /// <summary>收集一个钥匙（计数 +1，集齐时触发 onAllKeysCollected）。</summary>
        public void CollectKey()
        {
            collectedKeys++;
            if (collectedKeys >= totalKeys)
            {
                hasKey = true;
                onAllKeysCollected?.Invoke();
            }
        }

        /// <summary>重置钥匙收集计数。</summary>
        public void ResetKeys()
        {
            collectedKeys = 0;
            hasKey = false;
        }

        // ---------------- 状态标记列表 ----------------

        /// <summary>把指定索引的标记设为 value（列表长度不够时自动补齐）。</summary>
        public void SetFlag(int index, bool value)
        {
            if (index < 0) return;
            while (flags.Count <= index) flags.Add(false);
            flags[index] = value;
        }

        /// <summary>翻转指定索引的标记（列表长度不够时自动补齐）。</summary>
        public void ToggleFlag(int index)
        {
            if (index < 0) return;
            while (flags.Count <= index) flags.Add(false);
            flags[index] = !flags[index];
        }

        /// <summary>读取指定索引的标记，越界返回 false。</summary>
        public bool GetFlag(int index)
        {
            return index >= 0 && index < flags.Count && flags[index];
        }

        // ---------------- 开关门 ----------------

        /// <summary>开门：把门标记为开启，并失活 <see cref="doorTarget"/> 指定的物体。</summary>
        public void OpenDoor()
        {
            isDoorOpen = true;
            if (doorTarget != null) doorTarget.SetActive(false);
        }

        /// <summary>开门，并失活指定的物体。</summary>
        /// <param name="objectToDeactivate">门开启时要失活的物体</param>
        public void OpenDoor(GameObject objectToDeactivate)
        {
            isDoorOpen = true;
            if (objectToDeactivate != null) objectToDeactivate.SetActive(false);
        }

        /// <summary>关门：恢复 <see cref="doorTarget"/> 指定的物体。</summary>
        public void CloseDoor()
        {
            isDoorOpen = false;
            if (doorTarget != null) doorTarget.SetActive(true);
        }
    }
}
