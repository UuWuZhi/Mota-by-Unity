using System;
using Modules.EventNodeSystem.DataDefine;
using Modules.EventNodeSystem.DataDefine.Context;
using Modules.EventNodeSystem.DataDefine.Runner;
using Modules.EventNodeSystem.Runtime.Nodes.Flow.Data;
using UnityEngine;

namespace Modules.EventNodeSystem.Runtime.Nodes.Flow
{
    /// <summary>
    ///     已弃用的 If 节点：运行时不再参与执行，保留仅用于旧资产兼容与日志提示。
    /// </summary>
    [Obsolete("If 节点形式已弃用，请使用 Jump + 单条件节点 + 预制模板展开替代。")]
    [CreateAssetMenu(fileName = "If", menuName = "EventNodes/Flow/If")]
    public class IfNode : EventNode, IRunnerExecutionHintProvider
    {
        /// <summary>
        ///     弃用节点的执行提示：按同步立即处理，仅用于兼容旧调用路径。
        /// </summary>
        public RunnerExecutionHint GetExecutionHint()
        {
            return RunnerExecutionHint.SyncImmediate;
        }

        /// <summary>
        ///     弃用节点的执行入口：不再执行真实逻辑，仅输出兼容日志并跳过。
        /// </summary>
        public override void Execute(BaseNodeData data, EventNodeContext ctx, System.Action onComplete)
        {
            Debug.LogWarning("IfNode: 节点形式已弃用，已跳过执行。请使用 Jump + 单条件节点的平铺结构。");
            onComplete?.Invoke();
        }
    }
}
