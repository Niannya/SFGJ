using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SFGJ.Level;

namespace SFGJ.LevelEditor
{
    /// <summary>
    /// 在当前场景放一个可配置的场景过渡器（淡入淡出 + 停顿感参数可在这里调）。
    /// 不挂也能用：SceneTransitionFader.GetOrCreate() 会在运行时自动创建。
    /// 菜单：SFGJ → 场景 → 创建场景过渡器。
    /// </summary>
    public static class SceneTransitionSetup
    {
        [MenuItem("SFGJ/场景/创建场景过渡器", priority = 5)]
        public static void Create()
        {
            var existing = Object.FindObjectOfType<SceneTransitionFader>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                Debug.Log("[过渡] 场景里已有 SceneTransitionFader，已选中：" + existing.gameObject.name);
                return;
            }

            var go = new GameObject("SceneTransitionFader");
            go.AddComponent<SceneTransitionFader>();
            Undo.RegisterCreatedObjectUndo(go, "创建场景过渡器");
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[过渡] 已在当前场景创建 SceneTransitionFader（Inspector 里可调 fadeSeconds / blackoutHold）。");
        }
    }
}
