using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SFGJ.Level;

namespace SFGJ.LevelEditor
{
    /// <summary>
    /// 一键把「按 R 重置推块」脚本挂到玩家身上。
    /// 菜单：SFGJ → 场景 → 给玩家挂推块重置。
    /// </summary>
    public static class PushBlockResetterSetup
    {
        [MenuItem("SFGJ/场景/给玩家挂推块重置", priority = 6)]
        public static void Setup()
        {
            var player = Object.FindObjectOfType<PlayerController>();
            if (player == null)
            {
                Debug.LogError("[推块重置] 场景里找不到玩家（PlayerController）。");
                return;
            }

            if (player.GetComponent<PushBlockResetter>() == null)
            {
                player.gameObject.AddComponent<PushBlockResetter>();
                Debug.Log("[推块重置] 已给玩家挂上 PushBlockResetter（按 R 重置推块）。");
            }
            else
            {
                Debug.Log("[推块重置] 玩家已有 PushBlockResetter，跳过。");
            }

            EditorUtility.SetDirty(player.gameObject);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }
}
