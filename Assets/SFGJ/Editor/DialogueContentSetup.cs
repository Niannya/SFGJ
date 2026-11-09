using UnityEditor;
using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace SFGJ.LevelEditor
{
    /// <summary>
    /// 配置 R6 剧情对话到对话数据库（Assets/Dialogue/MainDialogueDatabase.asset）。
    /// 新增演员：母亲 / 医生 / 哥哥；新增对话：母亲责骂 / 医生安慰 / 医生继续 / 哥哥交予 / 哥哥普通。
    /// 菜单：SFGJ → 迷宫 → 配置剧情对话。
    /// </summary>
    public static class DialogueContentSetup
    {
        private const string DatabasePath = "Assets/Dialogue/MainDialogueDatabase.asset";

        [MenuItem("SFGJ/迷宫/配置剧情对话", priority = 4)]
        public static void Setup()
        {
            var database = AssetDatabase.LoadAssetAtPath<DialogueDatabase>(DatabasePath);
            if (database == null)
            {
                Debug.LogError("[剧情] 找不到对话数据库：" + DatabasePath);
                return;
            }

            var template = Template.FromDefault();
            const int playerId = 1; // Player actor

            // ---- 演员 ----
            var mother = EnsureActor(database, template, 3, "母亲", false);
            var doctor = EnsureActor(database, template, 4, "医生", false);
            var brother = EnsureActor(database, template, 5, "哥哥", false);

            // ---- 对话 ----
            AddConversation(database, template, 7, "母亲责骂", mother.id, playerId,
                "我怎么跟你说的？一天天的就知道玩！\n你以为你的成绩很好了吗！？稍微有点成绩就得意忘形！\n你这种性格，将来注定只能被别人踩在脚下！\n……\n把垃圾收拾进垃圾桶里，再把房间打扫一下。\n作业没做完之前不许睡觉！");

            AddConversation(database, template, 3, "医生安慰", doctor.id, playerId,
                "这是你曾经经历过的事，对吧？\n不过当时可不会有看不见的大姐姐帮你把娃娃拼好。\n你一定很难过吧。");

            AddConversation(database, template, 4, "医生继续", doctor.id, playerId,
                "继续前进吧，我感觉快到终点了。");

            AddConversation(database, template, 5, "哥哥交予", brother.id, playerId,
                "……谢谢。娃娃、拼好了。\n我还以为，再也不会有人理我了。\n（哥哥接过娃娃，紧紧抱在怀里。）");

            AddConversation(database, template, 6, "哥哥普通", brother.id, playerId,
                "（哥哥低着头，一言不发。）\n地上的碎片……还能拼回去吗？");

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log("[剧情] 已配置母亲 / 医生 / 哥哥的对话到对话数据库。");
        }

        private static Actor EnsureActor(DialogueDatabase database, Template template, int id, string name, bool isPlayer)
        {
            var existing = database.GetActor(id);
            if (existing != null) return existing;

            var actor = template.CreateActor(id, name, isPlayer);
            database.actors.Add(actor);
            return actor;
        }

        private static void AddConversation(DialogueDatabase database, Template template, int id, string title, int actorId, int conversantId, string text)
        {
            if (database.GetConversation(id) != null) return;

            var conversation = template.CreateConversation(id, title);
            conversation.ActorID = actorId;
            conversation.ConversantID = conversantId;

            var start = template.CreateDialogueEntry(0, conversation.id, "START");
            start.ActorID = actorId;
            start.ConversantID = conversantId;

            var line = template.CreateDialogueEntry(1, conversation.id, title);
            line.ActorID = actorId;
            line.ConversantID = conversantId;
            line.DialogueText = text;

            start.outgoingLinks.Add(new Link(conversation.id, start.id, conversation.id, line.id));

            conversation.dialogueEntries.Add(start);
            conversation.dialogueEntries.Add(line);
            database.conversations.Add(conversation);
        }
    }
}
