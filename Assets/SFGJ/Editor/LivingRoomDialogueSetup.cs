using UnityEditor;
using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace SFGJ.LevelEditor
{
    /// <summary>
    /// 配置 R1「客厅」剧情对话到对话数据库（Assets/Dialogue/MainDialogueDatabase.asset）。
    /// 新增演员：父亲（F）/ 妹妹（S）；新增对话：客厅101 ~ 客厅106。
    /// 菜单：SFGJ → 迷宫 → 配置客厅剧情对话。
    /// </summary>
    public static class LivingRoomDialogueSetup
    {
        private const string DatabasePath = "Assets/Dialogue/MainDialogueDatabase.asset";

        [MenuItem("SFGJ/迷宫/配置客厅剧情对话", priority = 7)]
        public static void Setup()
        {
            var database = AssetDatabase.LoadAssetAtPath<DialogueDatabase>(DatabasePath);
            if (database == null)
            {
                Debug.LogError("[客厅剧情] 找不到对话数据库：" + DatabasePath);
                return;
            }

            var template = Template.FromDefault();
            const int playerId = 1;   // P
            const int motherId = 3;   // M（开场「？？？」即母亲）
            const int doctorId = 4;   // D
            const int brotherId = 5;  // B

            // 新增演员：父亲 / 妹妹
            var father = EnsureActor(database, template, 6, "父亲", false);
            var sister = EnsureActor(database, template, 7, "妹妹", false);

            // 客厅101：开场自动对话（普通）
            AddLines(database, template, 8, "客厅101", playerId,
                (motherId, "你要想明白，我这都是为了你好。"),
                (playerId, "……"));

            // 客厅102：开场变体（第一次从 R8 进 R1）
            AddLines(database, template, 9, "客厅102", playerId,
                (motherId, "你要想明白，我这都是为了你好。"),
                (playerId, "你才不是为了我好。"));

            // 客厅103：父亲和妹妹
            AddLines(database, template, 10, "客厅103", playerId,
                (father.id, "和叶，你不要怪爸爸，好吗？\n你妈妈以前还不是这样的，我们刚认识那会儿，她的脾气还远不如今天。\n只是这些年来我们都变了，人都会变的，和叶。\n这是个很简单的道理。"),
                (sister.id, "爸爸，你们不要吵架好不好？老师说过，吵架是不对的……"),
                (father.id, "和叶，你不要怪爸爸，爸爸太没用了。\n爸爸也想给你们一个安稳、幸福的生活。但是爸爸太没用了。\n和叶，你去妈妈那边吧。\n妈妈比爸爸强多了，她也比爸爸更需要你。\n乖，和叶，你去陪着妈妈吧。"));

            // 客厅104：母亲和哥哥
            AddLines(database, template, 11, "客厅104", playerId,
                (motherId, "你过来干什么，不是跟你说过别进厨房吗！\n菜刀掉下来砍掉你的脚可别怪我没提醒过你，出去出去！\n对了，作业写完了吗？"),
                (brotherId, "没、没有，妈妈——"),
                (motherId, "跟你说了多少次一回到家就要写作业，写完才能出来玩，你听不懂人话吗！？\n天天跑来跑去得像什么样！你看看你爸爸现在的样子，你以后要成为他那样的人吗！？\n唉，我当初真是瞎了眼了嫁给这样的人。\n——我跟你讲你现在的成绩，将来只能去大街上捡垃圾，懂吗？\n还在这傻站着干嘛？腿断了？感觉回房间写作业去！"));

            // 客厅105：医生 + 玩家选项 + 医生回应
            AddChoice(database, template, 12, "客厅105", playerId, doctorId,
                "哎呀哎呀，真是神奇啊。科技的力量很强大吧？\n就像真的一样呢。",
                "这里是哪里？",
                "现在是什么情况？",
                "这里大概是你的精神世界，或者说，你的记忆深处……\n来让我们看看这里都藏着什么吧。",
                "现在我们已经进入你的精神世界了，这里发生的一切都是你心灵的投影。\n哪怕看到不合常理的事也不要惊慌哦。");

            // 客厅106：全部读完后
            AddLines(database, template, 13, "客厅106", playerId,
                (playerId, "他们是我的父母，但是这两个还是是谁……\n我完全没有这两个孩子的记忆。\n这个小女孩，也不是小时候的我……"),
                (doctorId, "谁知道呢，继续探索这个空间吧，也许答案就藏在后面。\n顺带一提，你妈妈对孩子还真是不留情啊。\n尝试离开这个房间吧。"));

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log("[客厅剧情] 已配置客厅101~106（父亲/妹妹/母亲/哥哥/医生）到对话数据库。");
        }

        private static Actor EnsureActor(DialogueDatabase database, Template template, int id, string name, bool isPlayer)
        {
            var existing = database.GetActor(id);
            if (existing != null) return existing;

            var actor = template.CreateActor(id, name, isPlayer);
            database.actors.Add(actor);
            return actor;
        }

        /// <summary>线性对话：START → 台词0 → 台词1 → …</summary>
        private static void AddLines(
            DialogueDatabase database, Template template, int id, string title, int playerId,
            params (int actorId, string text)[] lines)
        {
            if (database.GetConversation(id) != null) return;
            if (lines == null || lines.Length == 0) return;

            int npcId = lines[0].actorId == playerId ? (lines.Length > 1 ? lines[1].actorId : playerId) : lines[0].actorId;

            var conversation = template.CreateConversation(id, title);
            conversation.ActorID = playerId;
            conversation.ConversantID = npcId;

            var start = template.CreateDialogueEntry(0, conversation.id, "START");
            start.ActorID = playerId;
            start.ConversantID = npcId;
            conversation.dialogueEntries.Add(start);

            DialogueEntry prev = start;
            for (int i = 0; i < lines.Length; i++)
            {
                var entry = template.CreateDialogueEntry(i + 1, conversation.id, title + "-" + (i + 1));
                entry.ActorID = lines[i].actorId;
                entry.ConversantID = lines[i].actorId == playerId ? npcId : playerId;
                entry.DialogueText = lines[i].text;
                prev.outgoingLinks.Add(new Link(conversation.id, prev.id, conversation.id, entry.id));
                conversation.dialogueEntries.Add(entry);
                prev = entry;
            }

            database.conversations.Add(conversation);
        }

        /// <summary>选项对话：START → 医生台词 → [选项1/选项2] → 回应1/回应2</summary>
        private static void AddChoice(
            DialogueDatabase database, Template template, int id, string title, int playerId, int doctorId,
            string introText, string choice1, string choice2, string response1, string response2)
        {
            if (database.GetConversation(id) != null) return;

            var conversation = template.CreateConversation(id, title);
            conversation.ActorID = playerId;
            conversation.ConversantID = doctorId;

            var start = template.CreateDialogueEntry(0, conversation.id, "START");
            start.ActorID = playerId;
            start.ConversantID = doctorId;

            var intro = template.CreateDialogueEntry(1, conversation.id, title + "-医生");
            intro.ActorID = doctorId;
            intro.ConversantID = playerId;
            intro.DialogueText = introText;

            var opt1 = template.CreateDialogueEntry(2, conversation.id, title + "-选项1");
            opt1.ActorID = playerId;
            opt1.ConversantID = doctorId;
            opt1.MenuText = choice1;
            opt1.DialogueText = choice1;

            var opt2 = template.CreateDialogueEntry(3, conversation.id, title + "-选项2");
            opt2.ActorID = playerId;
            opt2.ConversantID = doctorId;
            opt2.MenuText = choice2;
            opt2.DialogueText = choice2;

            var resp1 = template.CreateDialogueEntry(4, conversation.id, title + "-回应1");
            resp1.ActorID = doctorId;
            resp1.ConversantID = playerId;
            resp1.DialogueText = response1;

            var resp2 = template.CreateDialogueEntry(5, conversation.id, title + "-回应2");
            resp2.ActorID = doctorId;
            resp2.ConversantID = playerId;
            resp2.DialogueText = response2;

            start.outgoingLinks.Add(new Link(conversation.id, start.id, conversation.id, intro.id));
            intro.outgoingLinks.Add(new Link(conversation.id, intro.id, conversation.id, opt1.id));
            intro.outgoingLinks.Add(new Link(conversation.id, intro.id, conversation.id, opt2.id));
            opt1.outgoingLinks.Add(new Link(conversation.id, opt1.id, conversation.id, resp1.id));
            opt2.outgoingLinks.Add(new Link(conversation.id, opt2.id, conversation.id, resp2.id));

            conversation.dialogueEntries.Add(start);
            conversation.dialogueEntries.Add(intro);
            conversation.dialogueEntries.Add(opt1);
            conversation.dialogueEntries.Add(opt2);
            conversation.dialogueEntries.Add(resp1);
            conversation.dialogueEntries.Add(resp2);

            database.conversations.Add(conversation);
        }
    }
}
