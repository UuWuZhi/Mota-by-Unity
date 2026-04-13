using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

/// <summary>
/// 与 Yarn Spinner 集成的控制流节点
/// 将对话过程交由 Yarn 控制，执行结束后根据路由信号走向不同的分支
/// </summary>
[CreateAssetMenu(fileName = "YarnFlowNode", menuName = "EventNodes/Flow/YarnFlowNode")]
public class YarnFlowNode : EventNode
{
    [Serializable]
    public class YarnBranch
    {
        public string routeName;
        public EventNode nextNode;
    }

    [Tooltip("Yarn 文件中的节点名称")]
    public string startNode = "Start";

    [Tooltip("根据 Yarn 发出的 ens_route 信号，走向不同的后续节点")]
    public List<YarnBranch> branches = new List<YarnBranch>();

    [Tooltip("如果没有触发任何分支路由信号或者普通文本结束，则走向此默认节点")]
    public EventNode defaultNext;

    public override Type[] GetRequiredServices()
    {
        var set = new HashSet<Type> { typeof(YarnRouteBridge), typeof(DialogueRunner) };

        if (defaultNext != null)
        {
            var req = defaultNext.GetRequiredServices();
            if (req != null)
            {
                foreach (var t in req) if (t != null) set.Add(t);
            }
        }

        if (branches != null)
        {
            foreach (var b in branches)
            {
                if (b == null || b.nextNode == null) continue;
                var req = b.nextNode.GetRequiredServices();
                if (req == null) continue;
                foreach (var t in req) if (t != null) set.Add(t);
            }
        }

        var result = new Type[set.Count];
        set.CopyTo(result);
        return result;
    }

    public override void Execute(EventNodeContext ctx, Action onComplete)
    {
        if (ctx == null || ctx.OwnerMono == null)
        {
            onComplete?.Invoke();
            return;
        }

        ctx.OwnerMono.StartCoroutine(RunYarnDialogue(ctx, onComplete));
    }

    private IEnumerator RunYarnDialogue(EventNodeContext ctx, Action onComplete)
    {
        var bridge = ctx.GetService<YarnRouteBridge>();
        var runner = ctx.GetService<DialogueRunner>();

        if (bridge == null || runner == null)
        {
            Debug.LogError("YarnFlowNode: Missing YarnRouteBridge or DialogueRunner in context.");
            onComplete?.Invoke();
            yield break;
        }

        // 清理上一次的路由信号
        bridge.ClearRoute();

        // 启动 Yarn 对话
        runner.StartDialogue(startNode);

        // 等待对话结束
        while (runner.IsDialogueRunning)
        {
            yield return null;
        }

        // 获取路由信号
        string route = bridge.CurrentRoute;
        EventNode target = defaultNext;

        if (!string.IsNullOrEmpty(route) && branches != null)
        {
            foreach (var b in branches)
            {
                if (b.routeName == route)
                {
                    target = b.nextNode;
                    break;
                }
            }
        }

        // 执行后续节点
        if (target != null)
        {
            // 通过 StartCoroutine 延迟一帧执行，避免 UI 处理同帧的闪烁
            ctx.OwnerMono.StartCoroutine(ExecuteTargetNextFrame(target, ctx, onComplete));
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    private IEnumerator ExecuteTargetNextFrame(EventNode target, EventNodeContext ctx, Action onComplete)
    {
        yield return null;
        target.Execute(ctx, () => onComplete?.Invoke());
    }
}
