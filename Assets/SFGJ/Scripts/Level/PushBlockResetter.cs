using System.Collections.Generic;
using UnityEngine;

namespace SFGJ.Level
{
    /// <summary>
    /// 按 R 重置推块位置（挂在玩家身上）。
    /// 开局记录每个推块的初始位置，按下 R 时把推块移回原位；
    /// 可选一并重置推块触发器状态（让已经触发过的触发器能再次触发）。
    /// </summary>
    public class PushBlockResetter : MonoBehaviour
    {
        [Header("重置")]
        [Tooltip("按下 R 时是否连同重置推块触发器的状态")]
        public bool resetTriggers = true;

        [SerializeField]
        [Tooltip("重置键，默认 R")]
        private KeyCode resetKey = KeyCode.R;

        private readonly Dictionary<PushBlockBehaviour, Vector3> _initialPositions =
            new Dictionary<PushBlockBehaviour, Vector3>();

        private void Start()
        {
            CacheInitialPositions();
        }

        private void Update()
        {
            if (Input.GetKeyDown(resetKey))
            {
                ResetAll();
            }
        }

        private void CacheInitialPositions()
        {
            _initialPositions.Clear();
            foreach (var block in FindObjectsOfType<PushBlockBehaviour>())
            {
                _initialPositions[block] = block.transform.position;
            }
        }

        private void ResetAll()
        {
            foreach (var kv in _initialPositions)
            {
                if (kv.Key != null)
                    kv.Key.transform.position = kv.Value;
            }

            if (resetTriggers)
            {
                foreach (var trigger in FindObjectsOfType<PushTriggerBehaviour>())
                {
                    trigger.ResetState();
                }
            }
        }
    }
}
