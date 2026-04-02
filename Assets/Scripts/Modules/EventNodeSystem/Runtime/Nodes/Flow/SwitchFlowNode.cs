using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 多条件 Switch 节点：按顺序评估 case，匹配到第一个为 true 的 case 则执行对应分支；否则执行 defaultBranch（若有）
/// 每个 case 包含一个 ConditionNode 和对应的分支节点列表。
/// </summary>
[CreateAssetMenu(fileName = "SwitchFlowNode", menuName = "EventNodes/Flow/Switch")]
public class SwitchFlowNode : EventNode
{
    [Serializable]
    public class SwitchCase
    {
        public ConditionNode condition;
        public List<EventNode> branch = new List<EventNode>();
    }

    public List<SwitchCase> cases = new List<SwitchCase>();
    public List<EventNode> defaultBranch = new List<EventNode>();

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

        void CollectList(List<EventNode> nodes)
        {
            if (nodes == null) return;
            foreach (var node in nodes)
            {
                Collect(node);
            }
        }

        if (cases != null)
        {
            foreach (var sc in cases)
            {
                if (sc == null) continue;
                Collect(sc.condition);
                CollectList(sc.branch);
            }
        }

        CollectList(defaultBranch);

        var result = new Type[set.Count];
        set.CopyTo(result);
        return result;
    }

    public override void Execute(EventNodeContext ctx, Action onComplete)
    {
        if (cases == null || cases.Count == 0)
        {
            // 直接执行 defaultBranch
            ctx.OwnerMono?.StartCoroutine(RunBranch(ctx, defaultBranch, onComplete));
            return;
        }

        ctx.OwnerMono?.StartCoroutine(RunSwitch(ctx, onComplete));
    }

    private IEnumerator RunSwitch(EventNodeContext ctx, Action onComplete)
    {
        bool matched = false;
        int matchedIndex = -1;

        for (int i = 0; i < cases.Count; i++)
        {
            var sc = cases[i];
            if (sc == null || sc.condition == null) continue;

            bool finished = false;
            bool result = false;
            sc.condition.Evaluate(ctx, r =>
            {
                result = r;
                finished = true;
            });

            while (!finished) yield return null;

            if (result)
            {
                matched = true;
                matchedIndex = i;
                break;
            }
        }

        if (matched)
        {
            yield return ctx.OwnerMono.StartCoroutine(RunBranch(ctx, cases[matchedIndex].branch, onComplete));
        }
        else
        {
            yield return ctx.OwnerMono.StartCoroutine(RunBranch(ctx, defaultBranch, onComplete));
        }
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