using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SFGJ.LevelEditor
{
    /// <summary>
    /// 在 NineMapScene 里创建九宫格小地图框架（Canvas + 9 个占位 Image，带房间名文字），
    /// 并自动拖到 MapRingController.minimapRoot。
    /// 菜单：SFGJ → 场景 → 创建九宫格小地图框架。
    /// </summary>
    public static class MinimapBuilder
    {
        // tile 顺序（3x3 从上到下、从左到右）：房间号 + 房间名
        private static readonly (int room, string name)[] Tiles =
        {
            (6, "卧室"), (7, "游乐场"), (8, "卧室"),
            (5, "客厅"), (0, "诊疗室"), (1, "客厅"),
            (4, "教室"), (3, "游乐场"), (2, "教室"),
        };

        [MenuItem("SFGJ/场景/创建九宫格小地图框架", priority = 11)]
        public static void Build()
        {
            var ring = Object.FindObjectOfType<MapRingController>();
            if (ring == null)
            {
                Debug.LogError("[九宫格] 场景里找不到 MapRingController。");
                return;
            }

            // Canvas
            Transform canvasT = FindOrCreateCanvas();
            Transform grid = canvasT.Find("Grid");
            if (grid == null)
            {
                grid = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup)).transform;
                grid.SetParent(canvasT, false);
                var rect = grid.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(420f, 420f);
                var layout = grid.GetComponent<GridLayoutGroup>();
                layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                layout.constraintCount = 3;
                layout.cellSize = new Vector2(130f, 130f);
                layout.spacing = new Vector2(10f, 10f);
                layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
                layout.startAxis = GridLayoutGroup.Axis.Horizontal;
            }

            // 9 个占位 Image（带房间名文字）
            var tiles = new Image[9];
            for (int i = 0; i < 9; i++)
            {
                int room = Tiles[i].room;
                string label = "r" + room + "_" + Tiles[i].name;

                var child = grid.Find(label);
                if (child == null)
                {
                    child = new GameObject(label, typeof(RectTransform), typeof(Image)).transform;
                    child.SetParent(grid, false);
                    var img = child.GetComponent<Image>();
                    img.color = new Color(1f, 1f, 1f, 0.25f);

                    // 文字标记
                    var textGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
                    textGO.transform.SetParent(child, false);
                    var text = textGO.GetComponent<Text>();
                    text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    text.text = label;
                    text.fontSize = 22;
                    text.alignment = TextAnchor.MiddleCenter;
                    text.color = new Color(0.1f, 0.1f, 0.1f, 1f);
                    var tRect = textGO.GetComponent<RectTransform>();
                    tRect.anchorMin = Vector2.zero;
                    tRect.anchorMax = Vector2.one;
                    tRect.offsetMin = Vector2.zero;
                    tRect.offsetMax = Vector2.zero;
                }
                tiles[i] = child.GetComponent<Image>();
            }

            // 拖引用到 MapRingController.minimapRoot
            var so = new SerializedObject(ring);
            var rootProp = so.FindProperty("minimapRoot");
            if (rootProp.objectReferenceValue == null)
            {
                rootProp.objectReferenceValue = grid;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(ring);
                Debug.Log("[九宫格] 已创建九宫格并拖到 minimapRoot。");
            }
            else
            {
                Debug.Log("[九宫格] minimapRoot 已配置，跳过。");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[九宫格] 对应关系：左上r6卧室 上r7游乐场 右上r8卧室 / 左r5客厅 中r0诊疗室 右r1客厅 / 左下r4教室 下r3游乐场 右下r2教室。");
        }

        private static Transform FindOrCreateCanvas()
        {
            var existing = GameObject.Find("MinimapCanvas");
            if (existing != null) return existing.transform;

            var go = new GameObject("MinimapCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1001; // 必须高于 fadeOverlay(1000)，黑屏时才能显示
            return go.transform;
        }
    }
}
