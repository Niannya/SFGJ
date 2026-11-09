using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SFGJ.LevelEditor
{
    /// <summary>
    /// 一键把「可互动物体发光高亮」脚本挂到玩家身上。
    /// 菜单：SFGJ → 场景 → 给玩家挂交互高亮。
    /// </summary>
    public static class InteractableHighlighterSetup
    {
        private const string PlayerPrefabPath = "Assets/预制体/Player.prefab";

        [MenuItem("SFGJ/场景/给玩家挂交互高亮", priority = 7)]
        public static void Setup()
        {
            var player = Object.FindObjectOfType<PlayerController>();
            if (player == null)
            {
                Debug.LogError("[高亮] 场景里找不到玩家（PlayerController）。");
                return;
            }

            if (player.GetComponent<InteractableHighlighter>() == null)
            {
                player.gameObject.AddComponent<InteractableHighlighter>();
                Debug.Log("[高亮] 已给玩家挂上 InteractableHighlighter（面对可互动物体时发光）。");
            }
            else
            {
                Debug.Log("[高亮] 玩家已有 InteractableHighlighter，跳过。");
            }

            EditorUtility.SetDirty(player.gameObject);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        [MenuItem("SFGJ/场景/把交互高亮挂到 Player 预制体", priority = 8)]
        public static void SetupPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[高亮] 找不到 Player 预制体：" + PlayerPrefabPath);
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                if (instance.GetComponent<InteractableHighlighter>() == null)
                {
                    instance.AddComponent<InteractableHighlighter>();
                    PrefabUtility.SaveAsPrefabAsset(instance, PlayerPrefabPath);
                    Debug.Log("[高亮] 已把 InteractableHighlighter 挂到 Player 预制体（全项目通用）。");
                }
                else
                {
                    Debug.Log("[高亮] Player 预制体已有 InteractableHighlighter，跳过。");
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
