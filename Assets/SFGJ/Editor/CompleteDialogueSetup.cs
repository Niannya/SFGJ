using UnityEditor;
using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace SFGJ.LevelEditor
{
    /// <summary>
    /// 配置完整剧情对话到对话数据库（Assets/Dialogue/MainDialogueDatabase.asset）。
    /// 覆盖：Opening、R0、R2、R3、R4、R5、R7（R1=客厅、R6=卧室已单独配置）。
    /// 命名规则：去掉房间字母 r，用「房间号-序号」，Opening 特殊保留。
    /// 菜单：SFGJ → 迷宫 → 配置完整剧情对话。
    /// </summary>
    public static class CompleteDialogueSetup
    {
        private const string DatabasePath = "Assets/Dialogue/MainDialogueDatabase.asset";

        [MenuItem("SFGJ/迷宫/配置完整剧情对话", priority = 8)]
        public static void Setup()
        {
            var database = AssetDatabase.LoadAssetAtPath<DialogueDatabase>(DatabasePath);
            if (database == null)
            {
                Debug.LogError("[剧情] 找不到对话数据库：" + DatabasePath);
                return;
            }

            var template = Template.FromDefault();
            const int p = 1; // Player
            const int m = 3; // 母亲
            const int d = 4; // 医生
            const int b = 5; // 哥哥
            const int f = 6; // 父亲
            const int s = 7; // 妹妹

            var y = EnsureActor(database, template, 8, "迎迎", false);
            var k = EnsureActor(database, template, 9, "小孩", false);

            // ===== Opening =====
            AddLines(database, template, 14, "Opening", p,
                (p, "你见过鬼吗？呃，我是说，你有过被附身的经历吗？别误会，我并不是要呃，宣传一些神神鬼鬼的东西。但是——好吧。\n我本来也不相信鬼的存在。但自从我上了大学以后，有些我解释不清的事发生在身边。\n比如，明明没有喝酒却在第二天早上起来完全记不清昨晚发生了什么，比如经常回过神来发现就在手边的东西被移到了别处，再或者明明刚刚才接了一杯水端起杯子却发现是空的……之类的。\n我承认我平时确实不太小心啦……但，这些事显然不是我迷糊才发生的。毕竟，总不会有人在我上课的时候趁我不注意偷偷把我的水喝光再，呃，放回原处吧……\n不过这个世界上，据说有很多怪人存在。说不定会有那种喜欢偷喝别人的水的人也说不定，不过在我这十八年的人生中，我是没见过那种人……\n嗯，言归正传，总之这些事逐渐发展到了让我不能忽视的地步。我的朋友也说，我有时会突然像变了个人似的，而且根据观察，大概有两个人经常出现。\n一个是七岁左右的小女孩，另一个是十二岁左右的小男孩。他们分别说是我的妹妹和哥哥。\n我可不记得我的父母曾经给我生过哥哥或妹妹，更何况他们早就已经离婚了。\n呃，妹妹说不定有可能，如果他们在我不知道的地方复合了……什么的。嗯，果然还是不太可能。\n我仔细回想母亲的面貌，却发现已经想不起来了。算下来在她和父亲离婚后，我已经八年没见过她了。\n我似乎没法从脑子里揪出他们离婚的理由，那些琐事一直在脑中的迷雾中隐藏着自己的身形。\n不过我经常忘事啦，这也是我的一大特点。呃，说不定这些被我遗忘的记忆，说不定其实真的被鬼魂给夺走了？\n我把这些想法告诉朋友之后，她们都说我脑子有问题。\n这点我倒是不否认啦，再加上我心里或许隐隐觉得，我的忘性大不是天生的，或许其中藏着某些惊天阴谋。\n说不定我其实是穿梭时空在不同世界执行秘密任务的特工，但在一次任务之后不小心被敌人给抓住。敌人在我身上进行了惨无人道的实验，我记不住事便是那是的后遗症……\n我把这些想法告诉朋友之后，她们又说我脑子有问题。\n我知道啦，也没必要说两遍吧……\n总之在她们的建议下，我预约了心理医生。"));

            // ===== R0 =====
            AddLines(database, template, 15, "0-1", p,
                (d, "这是脑机，能够接入你的大脑然后——在这个屏幕上显示你脑中的画面。"),
                (p, "看到我脑中的画面，然后呢？"),
                (d, "然后——你打开那扇门试试。"));

            // ===== R2（教室：迎迎和妹妹）=====
            AddLines(database, template, 16, "2-1", p,
                (y.id, "小叶，周末来我家玩吧！我妈妈给我买了新的娃娃，很漂亮！"),
                (s, "迎迎，我……我妈妈不让我去别人家里玩，对不起……"),
                (y.id, "怎么这样，你妈妈是个坏蛋！那你周末只能在家吗？"),
                (s, "嗯……没有补习班的时候，只能待在家里，也不能看电视。"),
                (y.id, "那也太无聊了吧！如果是我一定会疯掉的。"),
                (s, "……\n呃，还好吧，一直都是这样的。"));

            AddLines(database, template, 17, "2-2", p,
                (y.id, "小叶，这个娃娃送给你！以后你在家无聊的时候，就和她玩吧！"),
                (s, "可是，可是……我妈妈不许我有玩具，她会丢掉的……"),
                (y.id, "没关系，你把她藏起来就好了！\n她很脆弱的，要保护好她哦！"));

            AddLines(database, template, 18, "2-3", p,
                (d, "哎呀，很可爱的娃娃呢。\n好像可以通过那扇门了呢，刚刚我试着打开它，结果我撞了个鼻青脸肿也没成功。\n真神奇呀这里。"));

            AddLines(database, template, 19, "2-4", p,
                (d, "这扇门很牢固呢，哎哎，痛痛痛。\n那边的小女孩貌似知道什么，去问问她吧。"));

            // ===== R3（游乐场）=====
            AddLines(database, template, 20, "3-1", p,
                (s, "爸爸！你看！摩天轮！"),
                (f, "和叶，开心吗？"),
                (s, "开心！爸爸，谢谢你和妈妈带我来游乐场！"),
                (f, "这都是和叶的努力的结果。\n和叶那么努力地考进了年级前三，我们当然要好好奖励一下。\n走吧，妈妈应该买好票了，和叶就高高兴兴地玩一天吧。"));

            AddLines(database, template, 21, "3-2", p,
                (d, "游乐场啊……好久没来了。下次也带女儿来吧。\n都看完了吗？没什么事的话就往前走吧。"));

            // ===== R4（教室：哥哥）=====
            AddLines(database, template, 22, "4-1", p,
                (b, "都说了不是我！不是我干的！\n你们误会了！\n我什么也没做！！！"));

            AddLines(database, template, 23, "4-2", p,
                (b, "我一定会找到。\n我一定要找到。"));

            AddLines(database, template, 24, "4-3", p,
                (b, "欸，奇怪，刚刚还没有的……是谁？\n不管了。"));

            AddLines(database, template, 25, "4-4", p,
                (b, "迎迎，给你，我找到了。"),
                (y.id, "谢谢你，萌兰……\n我知道，这一切不是你的错。"));

            AddLines(database, template, 26, "4-5", p,
                (d, "哎呀好险啊，你似乎改变了这个人的结局呢。\n如果他没找到的话……会发生什么呢？\n因为学习成绩好就被嫉妒，被同学冷落、诬陷乃至精神崩溃什么的……\n这种事，大概没有发生吧。"));

            AddLines(database, template, 27, "4-6", p,
                (d, "你也算做了一件善事呢。\n或许就这样一直前进下去，帮助更多的人，你也就能够多了解你自己了吧。\n接下来去下一个房间吧。"));

            // ===== R5（客厅：离婚）=====
            AddLines(database, template, 28, "5-1", p,
                (f, "和花，你可能知道，我们……我和妈妈两个人，也许要分开，一段时间。"),
                (m, "磨磨唧唧的，难怪你骗不到人买你的保险。\n和花，我和你爸要离婚了，你跟谁？"),
                (p, "离……婚？\n离婚？"),
                (f, "没错——离婚。"),
                (m, "对，离婚！"),
                (f, "和花，你喜欢爸爸还是妈妈？"),
                (m, "和花，你选谁？"),
                (f, "和花，爸爸，还是妈妈？"),
                (m, "和花，你选谁？"),
                (b, "别怕，和花，有哥哥保护你呢。"),
                (s, "姐姐，不哭，我会一直陪着你的！"),
                (d, "嗯，这下了解了。你父母离婚的时候你才，十岁吧？\n因为承受不了母亲的高压管教，父母双方对另一方的不满又剧烈地影响着你的精神，再加上在学校经受的霸凌……有两个人格从你心里分裂出来了。\n作为妹妹的和叶，是坚强的你试图保护自己——这种心理具象化的产物；\n而作为哥哥的萌兰，则回应了脆弱的你乞求着谁来保护自己的愿望。\n原来你的妹妹和哥哥都是真实存在的，只不过，他们都是你自己。\n你将所有美好的记忆都留给了需要保护的妹妹，而为保护你们而生的哥哥则承担着所有痛苦的记忆。\n你自己所拥有的，则是说不上幸福，也说不上不幸的，作为一个普通人生活着的平淡记忆。\n嗯，后面似乎还有空间呢。至于你要如何处理他们，就看你自己的选择了。"));

            // ===== R7（游乐场：离开）=====
            AddLines(database, template, 29, "7-1", p,
                (m, "玩够了吧，玩够了就回去写作业。\n也不是我说你，才考个年级第三还有脸出来玩。\n你前面还有两个人呢！\n再说了，你这成绩在全县怎么样？全市呢？全省呢？\n我要是你，才考这么点分我都恨不得找个地洞钻进去。\n哎真是，烂泥扶不上墙。和你爸一个德行。"),
                (f, "……"));

            AddLines(database, template, 30, "7-2", p,
                (k.id, "我的气球！我的气球！"));

            AddLines(database, template, 31, "7-3", p,
                (m, "哎呀什么东西，今天出来真倒霉。\n赶紧回家了！还站着干什么！"));

            AddLines(database, template, 32, "7-4", p,
                (d, "那边的小孩好像很着急呀，或许有什么事要你做。"));

            AddLines(database, template, 33, "7-5", p,
                (d, "你哥哥似乎有话要说，去找找他吧。"));

            AddLines(database, template, 34, "7-6", p,
                (b, "谢谢你帮了我那么多……\n虽然本来应该是我来保护你们的，结果到头来是你保护了我。\n说起来，你长大了啊。太好了……"),
                (p, "……\n谢谢你帮我承担了那么多痛苦的记忆，否则我也许……"),
                (b, "接下来，你去找妹妹吧……\n她还看不见你呢。"));

            AddLines(database, template, 35, "7-7", p,
                (d, "看起来他已经看清事实了，接下来是妹妹了。\n前进吧！"));

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log("[剧情] 已配置 Opening / R0 / R2 / R3 / R4 / R5 / R7 全部对话到对话数据库。");
        }

        private static Actor EnsureActor(DialogueDatabase database, Template template, int id, string name, bool isPlayer)
        {
            var existing = database.GetActor(id);
            if (existing != null) return existing;

            var actor = template.CreateActor(id, name, isPlayer);
            database.actors.Add(actor);
            return actor;
        }

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
    }
}
