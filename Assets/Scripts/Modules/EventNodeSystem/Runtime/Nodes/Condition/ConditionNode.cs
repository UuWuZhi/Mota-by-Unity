using System;
using Modules.EventNodeSystem.DataDefine;
using Modules.EventNodeSystem.DataDefine.Context;

namespace Modules.EventNodeSystem.Runtime.Nodes.Condition
{
    /// <summary>
    ///     条件节点基类：同时支持带数据与不带数据的条件评估入口。
    ///     运行时优先使用带数据的评估入口，旧版无数据入口仅用于兼容。
    /// </summary>
    public abstract class ConditionNode : EventNode
    {
        /// <summary>
        ///     使用节点数据执行条件评估。
        /// </summary>
        /// <param name="data">节点数据。</param>
        /// <param name="ctx">执行上下文。</param>
        /// <param name="onResult">条件结果回调。</param>
        public abstract void Evaluate(BaseNodeData data, EventNodeContext ctx, Action<bool> onResult);

        /// <summary>
        ///     执行条件节点。
        ///     运行时条件节点不应作为独立执行逻辑；条件通常由前置的 Jump 捕获并评估。
        ///     为避免重复评估，执行时直接完成。
        /// </summary>
        /// <param name="data">节点数据。</param>
        /// <param name="ctx">执行上下文。</param>
        /// <param name="onComplete">完成回调。</param>
        public override void Execute(BaseNodeData data, EventNodeContext ctx, System.Action onComplete)
        {
            // 条件节点在运行时由 Jump 捕捉并独立评估；作为独立指令执行没有意义，直接完成以避免重复执行。
            onComplete?.Invoke();
        }
    }
}