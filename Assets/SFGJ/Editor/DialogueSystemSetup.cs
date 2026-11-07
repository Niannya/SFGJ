using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using PixelCrushers.DialogueSystem;
using SFGJ;

namespace SFGJ.EditorTools
{
    /// <summary>
    /// 一键把 Pixel Crushers Dialogue System 集成到当前场景：
    /// 实例化 Dialogue Manager、确保 EventSystem、创建对话数据库与一段开场对话，
    /// 并放置开局脚本，使游戏启动后自动显示对话 UI。
    /// </summary>
    public static class DialogueSystemSetup
    {
        private const string DialogueManagerPrefabPath =
            "Assets/Plugins/Pixel Crushers/Dialogue System/Prefabs/Dialogue Manager.prefab";

        [MenuItem("SFGJ/集成对话系统")]
        public static void Setup()
        {
            // 1. Dialogue Manager（不存在则从 prefab 实例化）
            var controller = Object.FindObjectOfType<DialogueSystemController>();
            if (controller == null)
            {
                var prefab = AssetDatabase.LoadMainAssetAtPath(DialogueManagerPrefabPath);
                if (prefab == null)
                {
                    EditorUtility.DisplayDialog("找不到 Prefab",
                        "找不到 Dialogue Manager.prefab，请确认 Dialogue System 插件的路径未被移动。", "OK");
                    return;
                }
                var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                controller = (instance != null) ? instance.GetComponentInChildren<DialogueSystemController>() : null;
            }

            if (controller == null)
            {
                EditorUtility.DisplayDialog("失败", "无法创建 Dialogue Manager。", "OK");
                return;
            }

            // 2. EventSystem（Canvas UI 交互需要）
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            // 3. 项目专用剧情 UI（带头像框；已生成时保留手动修改）
            var dialogueUI = DialogueUIBuilder.EnsureCustomDialogueUI();
            if (dialogueUI == null)
            {
                EditorUtility.DisplayDialog("失败", "无法创建项目剧情对话 UI。", "OK");
                return;
            }
            DialogueUIBuilder.ApplyToScene(controller, dialogueUI);

            // 4. 对话数据库（含开场对话；已存在时不覆盖）
            if (controller.initialDatabase == null)
            {
                controller.initialDatabase = CreateDatabaseWithOpening();
            }

            // 5. 开局脚本（自动开始开场对话，显示对话 UI）
            if (Object.FindObjectOfType<OpeningDialogue>() == null)
            {
                var go = new GameObject("Opening Dialogue");
                go.AddComponent<OpeningDialogue>();
                Undo.RegisterCreatedObjectUndo(go, "创建开局对话脚本");
            }

            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = controller.gameObject;

            Debug.Log("[对话系统] 集成完成：Dialogue Manager + EventSystem + 剧情 UI（带头像框）+ 对话数据库已就绪，Play 后自动显示对话 UI。");
        }

        private static DialogueDatabase CreateDatabaseWithOpening()
        {
            // 创建含 Player actor 与 Alert 变量的空数据库
            var database = DialogueSystemMenuItems.CreateDialogueDatabaseInstance();
            AddOpeningConversation(database);

            string dir = "Assets/SFGJ/Dialogue";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string path = AssetDatabase.GenerateUniqueAssetPath(dir + "/Dialogue Database.asset");
            AssetDatabase.CreateAsset(database, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(database);
            return database;
        }

        /// <summary>
        /// 在数据库中加一段最简开场对话：START -> 一句旁白。
        /// 之后可在 Dialogue Editor 里继续编辑这段对话或新增对话。
        /// </summary>
        private static void AddOpeningConversation(DialogueDatabase database)
        {
            var template = Template.FromDefault();
            const int playerId = 1; // CreateDialogueDatabaseInstance 已创建 Player（id=1）

            var conversation = template.CreateConversation(1, "Opening");

            var start = template.CreateDialogueEntry(0, conversation.id, "START");
            start.ActorID = playerId;
            start.ConversantID = playerId;

            var line = template.CreateDialogueEntry(1, conversation.id, "欢迎");
            line.ActorID = playerId;
            line.ConversantID = playerId;
            line.DialogueText = "……欢迎来到这个世界。";

            start.outgoingLinks.Add(new Link(conversation.id, start.id, conversation.id, line.id));

            conversation.dialogueEntries.Add(start);
            conversation.dialogueEntries.Add(line);
            database.conversations.Add(conversation);
        }
    }
}
