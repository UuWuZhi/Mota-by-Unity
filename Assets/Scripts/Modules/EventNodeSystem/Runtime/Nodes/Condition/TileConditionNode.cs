using System;
using Modules.Core.Runtime;
using Modules.EventNodeSystem.DataDefine;
using Modules.EventNodeSystem.DataDefine.Context;

namespace Modules.EventNodeSystem.Runtime.Nodes.Condition
{
    /// <summary>
    ///     瓦片专用条件模板：统一校验上下文类型，并将强类型上下文交给子类处理。
    /// </summary>
    public abstract class TileConditionNode : ConditionNode
    {
        /// <summary>
        ///     使用节点数据与瓦片上下文执行条件评估。
        /// </summary>
        /// <param name="data">节点数据。</param>
        /// <param name="ctx">瓦片上下文。</param>
        /// <param name="onResult">条件结果回调。</param>
        public override void Evaluate(BaseNodeData data, EventNodeContext ctx, Action<bool> onResult)
        {
            if (ctx is EventNodeTileContext tileCtx)
            {
                try
                {
                    EvaluateTile(data, tileCtx, onResult);
                }
                catch (Exception ex)
                {
                    DebugEditor.LogException(ex);
                    DebugEditor.LogError($"[{nameof(TileConditionNode)}]: 执行 {GetType().Name} 时发生异常。");
                    onResult?.Invoke(false);
                }
            }
            else
            {
                DebugEditor.LogWarning(
                    $"{GetType().Name}: 需要 EventNodeTileContext，但收到 {ctx?.GetType().Name ?? "null"}，默认返回 false。");
                onResult?.Invoke(false);
            }
        }


        /// <summary>
        ///     子类实现瓦片专用条件判定逻辑。
        /// </summary>
        /// <param name="data">节点数据。</param>
        /// <param name="ctx">瓦片上下文。</param>
        /// <param name="onResult">条件结果回调。</param>
        public abstract void EvaluateTile(BaseNodeData data, EventNodeTileContext ctx, Action<bool> onResult);
    }
}