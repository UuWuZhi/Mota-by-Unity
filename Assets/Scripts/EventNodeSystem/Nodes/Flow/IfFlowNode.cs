using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// If 流程：第一个节点必须是 ConditionNode（或外部指定），根据条件选择执行 trueBranch 或 falseBranch
/// </summary>
[CreateAssetMenu(fileName = "IfNode", menuName = "EventNodes/Flow/If")]
public class IfFlowNode : EventNode
{
    public ConditionNode condition;
    public List<EventNode> trueBranch = new List<EventNode>();
    public List<EventNode> falseBranch = new List<EventNode>();

    public override void Execute(EventNodeContext ctx, Action onComplete)
    {
        if (condition == null)
        {
            // 没有条件则直接完成
            onComplete?.Invoke();
            return;
        }
        condition.Evaluate(ctx, result =>
        {
            ctx.OwnerMono?.StartCoroutine(RunBranch(ctx, result ? trueBranch : falseBranch, onComplete));
        });
    }

    private IEnumerator RunBranch(EventNodeContext ctx, List<EventNode> branch, Action onComplete)
    {
        foreach (var node in branch)
        {
            bool finished = false;
            node?.Execute(ctx, () => { finished = true; });
            while (!finished) yield return null;
        }
        onComplete?.Invoke();
    }
}