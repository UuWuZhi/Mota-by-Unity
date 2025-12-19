using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 流程节点（顺序执行）
/// 子节点可以是 Condition/Action/Flow 等。默认顺序执行，遇到 Condition 节点时，将其评估并当作 gate（如果需要分支请用 IfFlowNode）
/// </summary>
[CreateAssetMenu(fileName = "SequenceNode", menuName = "EventNodes/Flow/Sequence")]
[Serializable]
public class SequenceFlowNode : EventNode
{
    public List<EventNode> children = new List<EventNode>();

    public override void Execute(EventNodeContext ctx, Action onComplete)
    {
        ctx.OwnerMono?.StartCoroutine(RunSequence(ctx, onComplete));
    }

    private IEnumerator RunSequence(EventNodeContext ctx, Action onComplete)
    {
        for (int i = 0; i < children.Count; i++)
        {
            bool finished = false;
            children[i]?.Execute(ctx, () => { finished = true; });
            // wait until node calls onComplete
            while (!finished) yield return null;
        }
        onComplete?.Invoke();
    }
}