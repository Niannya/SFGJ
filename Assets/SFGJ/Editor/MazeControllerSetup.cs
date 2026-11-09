using UnityEditor;
using UnityEngine;
using SFGJ.Level;

namespace SFGJ.LevelEditor
{
    /// <summary>
    /// 一键在场景中创建 / 查找迷宫控制器对象，并挂载 MazeController 组件。
    /// 菜单：SFGJ → 迷宫 → 挂载迷宫控制器。
    /// </summary>
    public static class MazeControllerSetup
    {
        [MenuItem("SFGJ/迷宫/挂载迷宫控制器", priority = 2)]
        public static void Setup()
        {
            var existing = Object.FindObjectOfType<MazeController>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                Debug.Log("[迷宫] 场景中已存在迷宫控制器，已选中。");
                return;
            }

            var go = GameObject.Find("MazeController");
            if (go == null)
            {
                go = new GameObject("MazeController");
                Undo.RegisterCreatedObjectUndo(go, "创建迷宫控制器");
            }

            go.AddComponent<MazeController>();
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            Debug.Log("[迷宫] 已创建并挂载迷宫控制器。");
        }
    }
}
