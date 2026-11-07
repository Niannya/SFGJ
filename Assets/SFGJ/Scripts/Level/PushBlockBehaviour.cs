using UnityEngine;
using UnityEngine.Events;

namespace SFGJ.Level
{
    /// <summary>
    /// 推块组件（挂在 Pushable 元素上）。
    /// 玩家碰到推块时，推块向玩家推动的方向移动一格（一个 cellSize）；
    /// 若目标格被墙 / 门 / 其他推块挡住则不动。
    /// 碰撞器保持实心（不可穿过），靠玩家碰撞检测推动。
    /// </summary>
    public class PushBlockBehaviour : MonoBehaviour
    {
        [Header("推块")]
        [SerializeField]
        [Tooltip("单格移动距离（世界单位）；留 0 则自动读取关卡网格的 cellSize")]
        private float stepSize = 0f;

        [Tooltip("被推动一次后触发（可选，例如放音效）")]
        public UnityEvent onPushed;

        private float Step
        {
            get
            {
                if (stepSize > 0f) return stepSize;
                var settings = FindObjectOfType<LevelSettings>();
                return settings != null ? settings.cellSize : 1f;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            var player = collision.collider.GetComponent<PlayerController>();
            if (player == null) return;

            // 以玩家碰撞盒中心到推块中心的差，判断推动方向
            Vector2 push = (Vector2)transform.position - (Vector2)player.transform.position;
            TryMove(push);
        }

        private void TryMove(Vector2 pushDirection)
        {
            Vector2 dir = SnapDirection(pushDirection);
            if (dir == Vector2.zero) return;

            float step = Step;
            Vector2 from = transform.position;
            Vector2 target = from + dir * step;

            if (IsBlocked(target))
            {
                // 推到障碍物上：不移动（可选：给玩家一点反馈）
                return;
            }

            transform.position = target;
            onPushed?.Invoke();
        }

        /// <summary>把推力方向归到 4 方向（水平/垂直），取更接近的一轴。</summary>
        private static Vector2 SnapDirection(Vector2 push)
        {
            if (push.sqrMagnitude < 0.0001f) return Vector2.zero;
            if (Mathf.Abs(push.x) > Mathf.Abs(push.y))
                return new Vector2(Mathf.Sign(push.x), 0f);
            return new Vector2(0f, Mathf.Sign(push.y));
        }

        /// <summary>目标位置是否被墙 / 门 / 其他推块 / 实心 LevelElement 挡住。</summary>
        private bool IsBlocked(Vector2 target)
        {
            // 先检查同一关卡网格里的实心元素（墙/门/推块/钥匙/NPC）
            var settings = FindObjectOfType<LevelSettings>();
            if (settings != null)
            {
                var grid = settings.WorldToGrid(transform.position + (Vector3)(target - (Vector2)transform.position));
                foreach (var beh in FindObjectsOfType<LevelElementBehaviour>())
                {
                    if (beh == null || beh.gameObject == gameObject) continue;
                    if (settings.ElementRect(beh).Contains(grid))
                    {
                        if (LevelPalette.IsSolid(beh.type)) return true;
                    }
                }
            }

            // 物理兜底：目标位置附近若有非玩家实心碰撞体也挡住（墙等没挂 LevelElementBehaviour 的）
            Collider2D hit = Physics2D.OverlapPoint(target);
            return hit != null && hit.GetComponent<PlayerController>() == null && !hit.isTrigger;
        }
    }
}
