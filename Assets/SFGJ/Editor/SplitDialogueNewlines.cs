using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace SFGJ.LevelEditor
{
    /// <summary>
    /// 把对话库中所有「一段台词里带换行」的节点，按换行拆成多个对话节点（每行一段，玩家逐段继续）。
    /// 菜单：SFGJ → 迷宫 → 按换行拆分对话。
    /// </summary>
    public static class SplitDialogueNewlines
    {
        private const string DatabasePath = "Assets/Dialogue/MainDialogueDatabase.asset";

        [MenuItem("SFGJ/迷宫/按换行拆分对话", priority = 9)]
        public static void Split()
        {
            var database = AssetDatabase.LoadAssetAtPath<DialogueDatabase>(DatabasePath);
            if (database == null)
            {
                Debug.LogError("[拆分] 找不到对话数据库：" + DatabasePath);
                return;
            }

            var template = Template.FromDefault();
            int total = 0;

            foreach (var conversation in database.conversations)
            {
                // 该对话当前最大节点 id（新增节点从这里往后分配）
                int maxId = 0;
                foreach (var e in conversation.dialogueEntries)
                    maxId = Mathf.Max(maxId, e.id);

                // 快照遍历（因为会在循环里新增节点）
                var entries = new List<DialogueEntry>(conversation.dialogueEntries);
                foreach (var entry in entries)
                {
                    string text = entry.DialogueText;
                    if (string.IsNullOrEmpty(text) || !text.Contains('\n')) continue;

                    var segments = text.Split('\n')
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();

                    if (segments.Count <= 1)
                    {
                        entry.DialogueText = segments.Count == 1 ? segments[0] : "";
                        continue;
                    }

                    // 记录原来指向后续节点的链接，最后要接到「最后一个新节点」上
                    var originalLinks = entry.outgoingLinks.ToList();
                    entry.outgoingLinks.Clear();

                    // 第一段保留在原节点
                    entry.DialogueText = segments[0];

                    // 后续每段新建一个节点，链式连接
                    DialogueEntry prev = entry;
                    for (int i = 1; i < segments.Count; i++)
                    {
                        maxId++;
                        var next = template.CreateDialogueEntry(maxId, conversation.id, entry.Title + "-" + i);
                        next.ActorID = entry.ActorID;
                        next.ConversantID = entry.ConversantID;
                        next.DialogueText = segments[i];

                        prev.outgoingLinks.Add(new Link(conversation.id, prev.id, conversation.id, next.id));
                        conversation.dialogueEntries.Add(next);
                        prev = next;
                        total++;
                    }

                    // 把原来的后续链接改接到最后一个新节点
                    foreach (var link in originalLinks)
                    {
                        link.originDialogueID = prev.id;
                        prev.outgoingLinks.Add(link);
                    }
                }
            }

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log($"[拆分] 已按换行拆分对话，新增 {total} 个对话节点。");
        }
    }
}
