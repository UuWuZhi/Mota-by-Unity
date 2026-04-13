using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 在事件系统中展示一段对话并提供多个选项。
/// - 对话内容保存在该 Node 中（speaker/text）
/// - 每个选项可关联下一个 EventNode（若为 null 则表示结束）
/// 执行流程：
///  1. 创建临时 DialogueNode 并通过 DialogueManager 启动显示
///  2. 监听 DialogueManager 的 OnUIChoiceSelected / OnDialogueEnded
///  3. 当玩家选择后，延后一帧再结束当前对话并执行对应分支（若存在）
/// </summary>
[CreateAssetMenu(fileName = "ChoiceDialogueNode", menuName = "EventNodes/Action/ChoiceDialogueNode")]
public class ChoiceDialogueNode : EventNode
{
    [Serializable]
    public class ChoiceEntry
    {
        public string Text;
        public EventNode NextNode; // 选择后执行的节点（可为 null）
    }

    public string speaker;
    [TextArea(2, 6)]
    public string text;
    public List<ChoiceEntry> choices = new();

    public override Type[] GetRequiredServices()
    {
        var set = new HashSet<Type>();

        if (choices != null)
        {
            foreach (var c in choices)
            {
                if (c == null || c.NextNode == null) continue;
                var req = c.NextNode.GetRequiredServices();
                if (req == null) continue;
                foreach (var t in req)
                {
                    if (t != null) set.Add(t);
                }
            }
        }

        var result = new Type[set.Count];
        set.CopyTo(result);
        return result;
    }

    public override void Execute(EventNodeContext ctx, Action onComplete)
    {
        // 暂不支持 ENS 与 Yarn 的集成，为了让游戏可编译，默认走第一个分支或不走
        Debug.LogWarning($"[选择对话，未接入Yarn] 说话者：{speaker}，内容：{text}");
        var target = (choices != null && choices.Count > 0) ? choices[0].NextNode : null;
        if (target != null)
        {
            target.Execute(ctx, () => onComplete?.Invoke());
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    private IEnumerator DelayedExecuteNextFrame(EventNodeContext ctx, EventNode target, Action onComplete)
    {
        yield return null; // 等待一帧，确保 UI 的 End 流程完成
        if (target != null)
        {
            target.Execute(ctx, () => onComplete?.Invoke());
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    private IEnumerator DelayedCompleteNextFrame(Action onComplete)
    {
        yield return null;
        onComplete?.Invoke();
    }
}