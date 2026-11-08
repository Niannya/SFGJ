using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SFGJ.LevelEditor
{
    /// <summary>
    /// 在当前场景创建一个「SceneSwitcher」控制器对象（带淡入切换场景的方法）。
    /// 菜单：SFGJ → 场景 → 添加场景切换控制器。
    /// </summary>
    public static class SceneSwitcherSetup
    {
        [MenuItem("SFGJ/场景/添加场景切换控制器", priority = 14)]
        public static void Setup()
        {
            var existing = Object.FindObjectOfType<SceneSwitcher>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("[场景切换] 场景里已有 SceneSwitcher：" + existing.gameObject.name);
                return;
            }

            var go = new GameObject("SceneSwitcher");
            go.AddComponent<SceneSwitcher>();
            Undo.RegisterCreatedObjectUndo(go, "添加场景切换控制器");
            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[场景切换] 已创建 SceneSwitcher 控制器（在 Inspector 里填 targetSceneName）。");
        }
    }
}
