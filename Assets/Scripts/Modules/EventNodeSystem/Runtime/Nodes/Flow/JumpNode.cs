using Modules.Core.Runtime;
using Modules.EventNodeSystem.DataDefine;
using Modules.EventNodeSystem.DataDefine.Context;
using Modules.EventNodeSystem.DataDefine.Runner;
using Modules.EventNodeSystem.Runtime.Nodes.Flow.Data;
using Modules.EventNodeSystem.Runtime.Runner;
using UnityEngine;

namespace Modules.EventNodeSystem.Runtime.Nodes.Flow
{
    [CreateAssetMenu(fileName = "Jump", menuName = "EventNodes/Flow/Jump")]
    public class JumpNode : EventNode, IRunnerExecutionHintProvider
    {
        public RunnerExecutionHint GetExecutionHint()
        {
            return RunnerExecutionHint.SyncImmediate;
        }

        public override void Execute(BaseNodeData data, EventNodeContext ctx, System.Action onComplete)
        {
            var jumpData = data as JumpData;
            if (jumpData == null)
            {
                DebugEditor.LogWarning("JumpNode: data 类型不匹配，跳过执行。");
                onComplete?.Invoke();
                return;
            }

            if (ctx != null && ctx.TryGetService<EventRunnerService>(out var runner))
                runner.JumpToLabel(jumpData.targetLabelName);
            else
                DebugEditor.LogWarning("JumpNode: 未找到 EventRunnerService，无法执行跳转。");

            onComplete?.Invoke();
        }
    }
}