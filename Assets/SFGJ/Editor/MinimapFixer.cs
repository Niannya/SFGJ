using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SFGJ.LevelEditor
{
    /// <summary>
    /// 修正九宫格：把 minimapRoot 的 Canvas sortingOrder 提到 1001（盖过黑屏）、
    /// 按房间对应关系重命名 9 张 Image、删除多余的 MinimapCanvas。
    /// 菜单：SFGJ → 场景 → 修正九宫格。
    /// </summary>
    public static class MinimapFixer
    {
        private static readonly string[] TileNames =
        {
            "r6_卧室", "r7_游乐场", "r8_卧室",
            "r5_客厅", "r0_诊疗室", "r1_客厅",
            "r4_教室", "r3_游乐场", "r2_教室",
        };

        [MenuItem("SFGJ/场景/修正九宫格", priority = 12)]
        public static void Fix()
        {
            var ring = Object.FindObjectOfType<MapRingController>();
            if (ring == null)
            {
                Debug.LogError("[九宫格] 找不到 MapRingController。");
                return;
            }

            var so = new SerializedObject(ring);
            var rootProp = so.FindProperty("minimapRoot");
            var root = rootProp.objectReferenceValue as Transform;
            if (root == null)
            {
                Debug.LogError("[九宫格] minimapRoot 未配置。");
                return;
            }

            // 1. sortingOrder 提到黑屏(1000)之上
            var canvas = root.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.sortingOrder < 1000)
            {
                canvas.sortingOrder = 1001;
                EditorUtility.SetDirty(canvas);
                Debug.Log($"[九宫格] 已把「{canvas.name}」sortingOrder 设为 1001。");
            }

            // 2. 按对应关系重命名 9 张 Image（按 hierarchy 顺序 = 视觉顺序）
            var imgs = root.GetComponentsInChildren<Image>(true);
            int n = Mathf.Min(9, imgs.Length);
            for (int i = 0; i < n; i++)
            {
                if (imgs[i].name != TileNames[i])
                {
                    imgs[i].name = TileNames[i];
                    EditorUtility.SetDirty(imgs[i].gameObject);
                }
            }
            Debug.Log("[九宫格] 已按顺序重命名 9 张图：" + string.Join(" / ", TileNames));

            // 3. 删除多余的 MinimapCanvas（如果 minimapRoot 不是它）
            var extra = GameObject.Find("MinimapCanvas");
            if (extra != null && extra.transform != root && extra.transform != root.parent)
            {
                Object.DestroyImmediate(extra);
                Debug.Log("[九宫格] 已删除多余的 MinimapCanvas。");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[九宫格] 对应关系（3x3）：r6卧室 r7游乐场 r8卧室 / r5客厅 r0诊疗室 r1客厅 / r4教室 r3游乐场 r2教室。");
        }
    }
}
