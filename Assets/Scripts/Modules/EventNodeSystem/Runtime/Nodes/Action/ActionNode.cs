using Modules.EventNodeSystem.DataDefine;
using Modules.EventNodeSystem.DataDefine.Context;
using Modules.EventNodeSystem.DataDefine.Runner;

namespace Modules.EventNodeSystem.Runtime.Nodes.Action
{
    /// <summary>
    ///     动作节点：执行原子行为（不做条件判断），完成时调用 onComplete
    /// </summary>
    public abstract class ActionNode : EventNode, IRunnerExecutionHintProvider
    {
        /// <summary>
        ///     默认执行提示：Action 节点按同步立即完成处理。
        ///     需要异步等待的节点请在子类覆写。
        /// </summary>
        public virtual RunnerExecutionHint GetExecutionHint()
        {
            return RunnerExecutionHint.SyncImmediate;
        }

        public abstract override void Execute(BaseNodeData data, EventNodeContext ctx, System.Action onComplete);
    }
}