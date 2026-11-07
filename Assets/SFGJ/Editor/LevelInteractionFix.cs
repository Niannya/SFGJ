using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SFGJ.Level;

namespace SFGJ.LevelEditor
{
    /// <summary>
    /// 一键修复 / 迁移运行时引用：
    ///   1. Player 的 interactionUI 指向场景中的 UIRoot（InteractionUI）。
    ///   2. 给元素按类型挂对应的功能组件（PortalBehaviour / KeyBehaviour / NPCBehaviour / TriggerBehaviour）。
    ///   3. 传送点（Portal）互相绑定 portalDestination。
    ///   4. 同步所有元素碰撞体 isTrigger（Key/NPC 实心，Portal/Trigger 可穿过）。
    /// 菜单：SFGJ → 迷宫 → 修复交互引用与传送点绑定。
    /// </summary>
    public static class LevelInteractionFix
    {
        [MenuItem("SFGJ/迷宫/修复交互引用与传送点绑定", priority = 3)]
        public static void Fix()
        {
            // 1. 修复 Player 的 interactionUI
            var playerInteraction = Object.FindObjectOfType<PlayerInteraction>();
            var interactionUI = Object.FindObjectOfType<InteractionUI>();
            if (playerInteraction != null && interactionUI != null)
            {
                var so = new SerializedObject(playerInteraction);
                var prop = so.FindProperty("interactionUI");
                if (prop != null && prop.objectReferenceValue == null)
                {
                    prop.objectReferenceValue = interactionUI;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(playerInteraction);
                    Debug.Log("[修复] Player.interactionUI → UIRoot");
                }
            }

            // 2. 给元素按类型挂对应的功能组件（旧结构迁移）
            foreach (var b in Object.FindObjectsOfType<LevelElementBehaviour>())
            {
                switch (b.type)
                {
                    case LevelElementType.Portal:
                        if (b.GetComponent<PortalBehaviour>() == null)
                            b.gameObject.AddComponent<PortalBehaviour>();
                        break;
                    case LevelElementType.Key:
                        {
                            var k = b.GetComponent<KeyBehaviour>();
                            if (k == null) k = b.gameObject.AddComponent<KeyBehaviour>();
                            if (string.IsNullOrWhiteSpace(k.conversationTitle)) k.conversationTitle = "第一次对话";
                            b.gameObject.layer = 6; // Interactable 层，让玩家按 Z 的 Raycast 能检测到
                            break;
                        }
                    case LevelElementType.NPC:
                        {
                            var n = b.GetComponent<NPCBehaviour>();
                            if (n == null) n = b.gameObject.AddComponent<NPCBehaviour>();
                            if (string.IsNullOrWhiteSpace(n.conversationTitle)) n.conversationTitle = "第一次对话";
                            b.gameObject.layer = 6; // Interactable 层，让玩家按 Z 的 Raycast 能检测到
                            break;
                        }
                    case LevelElementType.Trigger:
                        if (b.GetComponent<TriggerBehaviour>() == null)
                            b.gameObject.AddComponent<TriggerBehaviour>();
                        break;
                }
            }

            // 3. 传送点互相绑定
            var portals = new List<PortalBehaviour>(Object.FindObjectsOfType<PortalBehaviour>());
            if (portals.Count >= 2)
            {
                for (int i = 0; i < portals.Count; i++)
                {
                    var next = portals[(i + 1) % portals.Count];
                    portals[i].portalDestination = next.transform;
                    EditorUtility.SetDirty(portals[i]);
                    Debug.Log($"[修复] 传送点「{portals[i].name}」→ 「{next.name}」");
                }
            }
            else
            {
                Debug.LogWarning($"[修复] 场景里只有 {portals.Count} 个传送点，需要至少 2 个才能传送。");
            }

            // 4. 同步碰撞体 isTrigger
            foreach (var b in Object.FindObjectsOfType<LevelElementBehaviour>())
            {
                var col = b.GetComponent<Collider2D>();
                if (col == null) continue;
                bool shouldBeTrigger = !LevelPalette.IsSolid(b.type);
                if (col.isTrigger != shouldBeTrigger)
                {
                    col.isTrigger = shouldBeTrigger;
                    EditorUtility.SetDirty(col);
                    Debug.Log($"[修复] 「{b.name}」碰撞体 isTrigger = {shouldBeTrigger}");
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }
}
