using System;
using System.Collections;
using System.Linq;
using Modules.EventNodeSystem.DataDefine;
using Modules.EventNodeSystem.DataDefine.Context;
using Modules.EventNodeSystem.DataDefine.Runner;
using Modules.EventNodeSystem.Runtime.Nodes.Action.Data;
using UnityEngine;
using Yarn.Unity;

namespace Modules.EventNodeSystem.Runtime.Nodes.Action
{
    /// <summary>
    ///     与 Yarn Spinner 集成的控制流节点
    ///     将对话过程交由 Yarn 控制，执行结束后根据路由信号走向不同的分支
    /// </summary>
    [CreateAssetMenu(fileName = "YarnDialogue", menuName = "EventNodes/Action/YarnDialogue")]
    public class YarnDialogue : ActionNode
    {
        public override RunnerExecutionHint GetExecutionHint()
        {
            return RunnerExecutionHint.AsyncBlocking;
        }

        public override Type[] GetRequiredServices()
        {
            return new[] { typeof(YarnRouteBridge), typeof(DialogueRunner) };
        }

        public override void Execute(BaseNodeData data, EventNodeContext ctx, System.Action onComplete)
        {
            if (data is not YarnDialogueData yarnData)
            {
                Debug.LogWarning("YarnDialogue: data 类型不匹配，跳过执行。");
                onComplete?.Invoke();
                return;
            }

            if (ctx == null || !ctx.OwnerMono)
            {
                onComplete?.Invoke();
                return;
            }

            ctx.OwnerMono.StartCoroutine(RunYarnDialogue(yarnData, ctx, onComplete));
        }

        private IEnumerator RunYarnDialogue(YarnDialogueData data, EventNodeContext ctx, System.Action onComplete)
        {
            var bridge = ctx.GetService<YarnRouteBridge>();
            var runner = ctx.GetService<DialogueRunner>();

            if (bridge == null || !runner)
            {
                Debug.LogError("YarnDialogue: Missing YarnRouteBridge or DialogueRunner in context.");
                onComplete?.Invoke();
                yield break;
            }

            bridge.ClearRoute();
            runner.StartDialogue(data.startNode);

            while (runner.IsDialogueRunning) yield return null;

            var route = bridge.CurrentRoute;
            var target = data.defaultNext;
            var targetData = data.DefaultNextData;

            if (!string.IsNullOrEmpty(route) && data.branches != null)
                foreach (var b in data.branches.Where(b => b != null && b.routeName == route))
                {
                    target = b.nextNode;
                    targetData = b.NextData;
                    break;
                }

            if (target)
                ctx.OwnerMono.StartCoroutine(ExecuteTargetNextFrame(target, targetData, ctx, onComplete));
            else
                onComplete?.Invoke();
        }

        private IEnumerator ExecuteTargetNextFrame(EventNode target, BaseNodeData targetData, EventNodeContext ctx,
            System.Action onComplete)
        {
            yield return null;
            target.Execute(targetData, ctx, () => onComplete?.Invoke());
        }
    }
}