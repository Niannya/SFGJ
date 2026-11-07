using UnityEngine;

namespace SFGJ.Level
{
    /// <summary>
    /// 传送点组件（挂在 Portal 元素上）。
    /// 玩家进入传送点 → 若可传送，直接瞬移到 <see cref="portalDestination"/>。
    /// 碰撞器状态跟随传送开关：
    ///   - 可传送：碰撞器为触发器（可穿过，进入即传送）。
    ///   - 不可传送：碰撞器为实心（挡住玩家，无法穿过）。
    /// </summary>
    public class PortalBehaviour : MonoBehaviour
    {
        [Header("传送点")]
        [Tooltip("传送目标（另一个传送点的 Transform）")]
        public Transform portalDestination;

        [SerializeField]
        [Tooltip("是否允许传送")]
        private bool canTeleport = true;

        private Collider2D _collider;

        // 全局传送冷却（所有传送点共享，防止双向传送互相触发导致循环）
        private static float _lastTeleportTime = -10f;

        /// <summary>是否可传送（代码里读取 / 赋值都会同步碰撞器）。</summary>
        public bool CanTeleport
        {
            get => canTeleport;
            set
            {
                canTeleport = value;
                SyncCollider();
            }
        }

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            SyncCollider();
        }

        /// <summary>设置是否可传送，并同步碰撞器。</summary>
        public void SetTeleportEnabled(bool enabled)
        {
            CanTeleport = enabled;
        }

        /// <summary>根据传送开关同步碰撞器：可传送=触发器，不可传送=实心。</summary>
        private void SyncCollider()
        {
            var col = _collider != null ? _collider : GetComponent<Collider2D>();
            if (col != null) col.isTrigger = canTeleport;
        }

        // 编辑器里改 canTeleport 时同步碰撞器
        private void OnValidate()
        {
            if (Application.isPlaying) return;
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = canTeleport;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player == null) return;
            TryTeleport(other);
        }

        private void TryTeleport(Collider2D player)
        {
            if (!canTeleport) return;
            if (portalDestination == null) return;
            if (Time.time - _lastTeleportTime < 0.5f) return;

            _lastTeleportTime = Time.time;

            // 直接设置位置（teleport，不经过碰撞检测），确保瞬移成功
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.position = portalDestination.position;
            else
                player.transform.position = portalDestination.position;
        }
    }
}
