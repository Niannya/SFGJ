using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SFGJ.LevelEditor
{
    /// <summary>
    /// 给开始界面的 Button 挂 MenuButton（悬浮放大浮动 + 点击淡入黑屏切场景），并配置目标场景。
    /// 菜单：SFGJ → 场景 → 给开始按钮挂菜单脚本。
    /// </summary>
    public static class MenuButtonSetup
    {
        private const string TargetScene = "NineMapScene";

        [MenuItem("SFGJ/场景/给开始按钮挂菜单脚本", priority = 10)]
        public static void Setup()
        {
            var buttons = Object.FindObjectsOfType<Button>(true);
            if (buttons.Length == 0)
            {
                Debug.LogError("[菜单按钮] 场景里找不到 Button。");
                return;
            }

            foreach (var b in buttons)
            {
                var mb = b.GetComponent<MenuButton>();
                if (mb == null) mb = b.gameObject.AddComponent<MenuButton>();

                var so = new SerializedObject(mb);
                var sceneProp = so.FindProperty("sceneName");
                if (string.IsNullOrEmpty(sceneProp.stringValue))
                    sceneProp.stringValue = TargetScene;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(mb);
                Debug.Log($"[菜单按钮] 已给「{b.name}」挂 MenuButton，目标场景：{TargetScene}");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }
}
