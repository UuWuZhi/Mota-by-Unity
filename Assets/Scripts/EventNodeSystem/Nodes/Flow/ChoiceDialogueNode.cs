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
    public List<ChoiceEntry> choices = new List<ChoiceEntry>();

    public override void Execute(EventNodeContext ctx, Action onComplete)
    {

        var dm = ctx.DialogueManager;

        if (dm == null)
        {
            onComplete?.Invoke();
            return;
        }

        var runtimeData = new DialogueData
        {
            Id = $"choice_{Guid.NewGuid()}",
            Speaker = speaker,
            Text = text,
            Options = new List<DialogueOption>()
        };
        foreach (var c in choices)
        {
            runtimeData.Options.Add(new DialogueOption { Text = c.Text });
        }

        Action<int> onChoice = null;
        Action onDialogueEnded = null;

        onChoice = (idx) =>
        {
            // unsubscribe both handlers to avoid duplicate handling
            dm.OnUIChoiceSelected -= onChoice;
            dm.OnDialogueEnded -= onDialogueEnded;

            // end dialogue then execute selected branch after a frame
            dm.EndDialogue();

            var target = (idx >= 0 && idx < choices.Count) ? choices[idx].NextNode : null;

            if (ctx != null && ctx.OwnerMono != null)
            {
                ctx.OwnerMono.StartCoroutine(DelayedExecuteNextFrame(ctx, target, onComplete));
            }
            else
            {
                if (target != null) target.Execute(ctx, () => onComplete?.Invoke());
                else onComplete?.Invoke();
            }
        };

        onDialogueEnded = () =>
        {
            // unsubscribe both handlers
            dm.OnUIChoiceSelected -= onChoice;
            dm.OnDialogueEnded -= onDialogueEnded;

            // dialogue ended without choice (continue)
            if (ctx != null && ctx.OwnerMono != null)
            {
                ctx.OwnerMono.StartCoroutine(DelayedCompleteNextFrame(onComplete));
            }
            else
            {
                onComplete?.Invoke();
            }
        };

        dm.OnUIChoiceSelected += onChoice;
        dm.OnDialogueEnded += onDialogueEnded;

        dm.StartDialogue(runtimeData);
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