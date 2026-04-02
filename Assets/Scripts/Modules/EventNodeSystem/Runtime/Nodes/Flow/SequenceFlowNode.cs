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

    public override Type[] GetRequiredServices()
    {
        var set = new HashSet<Type>();

        void Collect(EventNode node)
        {
            if (node == null) return;
            var requirements = node.GetRequiredServices();
            if (requirements == null) return;
            foreach (var type in requirements)
            {
                if (type != null) set.Add(type);
            }
        }

        if (children != null)
        {
            foreach (var child in children)
            {
                Collect(child);
            }
        }

        var result = new Type[set.Count];
        set.CopyTo(result);
        return result;
    }

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