using UnityEngine;
using UnityEngine.Events;

namespace SFGJ.Level
{
    /// <summary>
    /// 传送点组件（挂在 Portal 元素上）。
    /// 玩家进入 → 若可传送，播放淡入淡出 + 黑屏停顿的过渡，在全黑中瞬移到目标传送点的落点。
    /// 过渡节奏由 <see cref="SceneTransitionFader"/> 提供（移植自 NineMapScene 的 MapRingController）。
    /// 碰撞器状态跟随传送开关：可传送 = 触发器，不可传送 = 实心挡路。
    /// </summary>
    public class PortalBehaviour : MonoBehaviour
    {
        [Header("传送点")]
        [Tooltip("传送目标（另一个传送点的 Transform）")]
        public Transform portalDestination;

        [Header("落点（可选）")]
        [Tooltip("别的传送点传过来时玩家落在哪里；留空则用自身中心。建议摆一个在触发区外的子对象，避免刚落地就被弹回去")]
        public Transform arrivalPoint;

        [Header("事件")]
        [Tooltip("传送发生时触发（在淡入淡出黑屏中瞬移的瞬间）")]
        public UnityEvent onTeleport;

        [SerializeField]
        [Tooltip("是否允许传送")]
        private bool canTeleport = true;

        [SerializeField, Min(0f)]
        [Tooltip("传送完成后多久内禁止再次触发（秒），防止双向传送点来回弹")]
        private float reentryCooldown = 0.8f;

        // 全局：上次完成传送的时间（所有传送点共享）
        private static float _lastArrivalTime = -10f;

        private bool _playerInside;
        private Collider2D _collider;

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

        private void SyncCollider()
        {
            var col = _collider != null ? _collider : GetComponent<Collider2D>();
            if (col != null) col.isTrigger = canTeleport;
        }

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
            if (_playerInside) return; // 已在触发区内，不重复触发
            _playerInside = true;
            TryTeleport(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() == null) return;
            _playerInside = false;
        }

        public void TryTeleport(Collider2D player)
        {
            if (!canTeleport) return;
            if (portalDestination == null) return;
            if (SceneTransitionFader.IsBusy) return; // 过渡中禁止重入
            if (Time.unscaledTime - _lastArrivalTime < reentryCooldown) return; // 刚传送完，冷却

            // 优先使用「目标传送点」上配置的落点，否则落在目标中心
            var targetPortal = portalDestination.GetComponent<PortalBehaviour>();
            Vector3 destination = (targetPortal != null && targetPortal.arrivalPoint != null)
                ? targetPortal.arrivalPoint.position
                : portalDestination.position;

            var rb = player.GetComponent<Rigidbody2D>();

            // 淡出 → 全黑停顿 → 在全黑里瞬移 → 淡入；期间玩家被冻结、输入被挡
            SceneTransitionFader.GetOrCreate().PlayTransition(
                () =>
                {
                    if (rb != null)
                    {
                        rb.velocity = Vector2.zero;
                        rb.position = destination;
                    }
                    player.transform.position = destination;
                    Physics2D.SyncTransforms();
                    onTeleport?.Invoke();
                },
                () => _lastArrivalTime = Time.unscaledTime);
        }
    }
}
