using UnityEngine;

namespace SFGJ.Level
{
    /// <summary>
    /// 门组件（挂在 Door 元素上）。
    /// Open() 开启门：让门（或指定物体）失活；Close() 恢复。
    /// </summary>
    public class DoorBehaviour : MonoBehaviour
    {
        [Tooltip("门开启时要失活的物体（默认是自己）")]
        public GameObject target;

        [Tooltip("门是否已开启")]
        public bool isOpen;

        /// <summary>开门：失活目标物体。</summary>
        public void Open()
        {
            isOpen = true;
            var t = target != null ? target : gameObject;
            if (t != null) t.SetActive(false);
        }

        /// <summary>关门：恢复目标物体。</summary>
        public void Close()
        {
            isOpen = false;
            var t = target != null ? target : gameObject;
            if (t != null) t.SetActive(true);
        }
    }
}
