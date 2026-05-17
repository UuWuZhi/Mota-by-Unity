using Modules.EventNodeSystem.DataDefine;
using Modules.EventNodeSystem.DataDefine.Context;
using Modules.EventNodeSystem.DataDefine.Runner;
using UnityEngine;

namespace Modules.EventNodeSystem.Runtime.Nodes.Flow
{
    [CreateAssetMenu(fileName = "Label", menuName = "EventNodes/Flow/Label")]
    public class LabelNode : EventNode, IRunnerExecutionHintProvider
    {
        public RunnerExecutionHint GetExecutionHint()
        {
            return RunnerExecutionHint.SyncImmediate;
        }

        public override void Execute(BaseNodeData data, EventNodeContext ctx, System.Action onComplete)
        {
            // 标签节点不执行任何逻辑，立即完成
            onComplete?.Invoke();
        }
    }
}