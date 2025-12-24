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
///  2. 监听场景中的 DialogueUI 的 OnChoiceClicked / OnContinueClicked
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
    [TextArea(2,6)]
    public string text;
    public List<ChoiceEntry> choices = new List<ChoiceEntry>();

    public override void Execute(EventNodeContext ctx, Action onComplete)
    {
        // 找到 UI
        var ui = GameObject.FindObjectOfType<UIDialogue>();
        var dm = DialogueManager.Instance;

        if (dm == null || ui == null)
        {
            // 无 UI 或 DialogueManager 时直接结束
            onComplete?.Invoke();
            return;
        }

        // 构造临时 DialogueNode 以供 UI 显示
        var runtimeNode = new DialogueNode
        {
            Id = $"choice_{Guid.NewGuid()}",
            Speaker = speaker,
            Text = text,
            Choices = new List<DialogueChoice>()
        };
        foreach (var c in choices)
        {
            runtimeNode.Choices.Add(new DialogueChoice { Text = c.Text, NextNodeId = null, ActionName = null });
        }

        // 事件处理器：用户选择某个索引
        Action<int> onChoice = null;
        Action onContinue = null;

        onChoice = (idx) =>
        {
            // 解除订阅
            ui.OnChoiceClicked -= onChoice;
            ui.OnContinueClicked -= onContinue;

            // 结束当前对话 UI（由本 Node 负责结束）
            dm.EndDialogue();

            // 获取目标节点引用
            var target = (idx >= 0 && idx < choices.Count) ? choices[idx].NextNode : null;

            // 延后一帧再执行下一个节点或完成回调，避免同帧 teardown/start 的竞态
            if (ctx != null && ctx.OwnerMono != null)
            {
                ctx.OwnerMono.StartCoroutine(DelayedExecuteNextFrame(ctx, target, onComplete));
            }
            else
            {
                // 没有 OwnerMono 时直接执行（保守回退）
                if (target != null) target.Execute(ctx, () => onComplete?.Invoke());
                else onComplete?.Invoke();
            }
        };

        // 若无选项（或玩家按继续），当作完成
        onContinue = () =>
        {
            ui.OnChoiceClicked -= onChoice;
            ui.OnContinueClicked -= onContinue;
            dm.EndDialogue();

            if (ctx != null && ctx.OwnerMono != null)
            {
                ctx.OwnerMono.StartCoroutine(DelayedCompleteNextFrame(onComplete));
            }
            else
            {
                onComplete?.Invoke();
            }
        };

        ui.OnChoiceClicked += onChoice;
        ui.OnContinueClicked += onContinue;

        // 启动对话显示
        dm.StartDialogue(runtimeNode);
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