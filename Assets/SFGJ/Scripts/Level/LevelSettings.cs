using UnityEngine;

namespace SFGJ.Level
{
    /// <summary>
    /// 挂载到场景中某个对象上，保存关卡网格配置，并提供网格 <-> 世界坐标换算。
    /// 关卡元素本身直接作为场景对象存在（挂在 LevelElementBehaviour 组件）。
    /// </summary>
    public class LevelSettings : MonoBehaviour
    {
        [Header("网格设置")]
        [Tooltip("地图宽度（格子数）")]
        public int gridWidth = 20;

        [Tooltip("地图高度（格子数）")]
        public int gridHeight = 12;

        [Tooltip("每个格子的大小（世界单位）")]
        public float cellSize = 1f;

        /// <summary>网格左下角的世界坐标（使整个网格以世界原点为中心）。</summary>
        public Vector2 GridOrigin
        {
            get { return new Vector2(-gridWidth * cellSize * 0.5f, -gridHeight * cellSize * 0.5f); }
        }

        /// <summary>格子坐标 -> 世界坐标（格子中心）。</summary>
        public Vector3 GridToWorld(Vector2Int grid)
        {
            Vector2 o = GridOrigin;
            float half = cellSize * 0.5f;
            return new Vector3(o.x + grid.x * cellSize + half, o.y + grid.y * cellSize + half, 0f);
        }

        /// <summary>世界坐标 -> 格子坐标。</summary>
        public Vector2Int WorldToGrid(Vector3 world)
        {
            Vector2 o = GridOrigin;
            int x = Mathf.FloorToInt((world.x - o.x) / cellSize);
            int y = Mathf.FloorToInt((world.y - o.y) / cellSize);
            return new Vector2Int(x, y);
        }

        /// <summary>元素中心的世界坐标（pos 为左下角格子）。</summary>
        public Vector3 ElementCenter(Vector2Int pos, Vector2Int size)
        {
            Vector2 c = pos + new Vector2(size.x, size.y) * 0.5f;
            Vector2 o = GridOrigin;
            return new Vector3(o.x + c.x * cellSize, o.y + c.y * cellSize, 0f);
        }

        /// <summary>元素占据的世界矩形。</summary>
        public Rect ElementWorldRect(Vector2Int pos, Vector2Int size)
        {
            Vector2 o = GridOrigin;
            Vector2 min = o + new Vector2(pos.x * cellSize, pos.y * cellSize);
            Vector2 s = new Vector2(size.x * cellSize, size.y * cellSize);
            return new Rect(min, s);
        }

        /// <summary>格子坐标是否在地图范围内。</summary>
        public bool InBounds(Vector2Int grid)
        {
            return grid.x >= 0 && grid.y >= 0 && grid.x < gridWidth && grid.y < gridHeight;
        }

        /// <summary>元素占据的格子矩形（左下角 + 尺寸）。</summary>
        public RectInt ElementRect(LevelElementBehaviour beh)
        {
            int w = Mathf.Max(1, beh.gridSize.x);
            int h = Mathf.Max(1, beh.gridSize.y);
            return new RectInt(beh.gridPos.x, beh.gridPos.y, w, h);
        }
    }
}
