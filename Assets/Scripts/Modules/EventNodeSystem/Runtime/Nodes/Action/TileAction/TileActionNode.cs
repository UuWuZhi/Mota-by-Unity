using Modules.EventNodeSystem.DataDefine;
using Modules.EventNodeSystem.DataDefine.Context;
using UnityEngine;

namespace Modules.EventNodeSystem.Runtime.Nodes.Action.TileAction
{
    /// <summary>
    ///     针对 Tile 的 ActionNode 模板：统一做类型判断并把强类型 ctx 交给 ExecuteTile 实现者处理。
    ///     保持向后兼容：不改变基类签名（重写 sealed Execute）
    /// </summary>
    public abstract class TileActionNode : ActionNode
    {
        // 禁止子类直接重写基类 Execute，以保证类型检查统一
        public sealed override void Execute(BaseNodeData data, EventNodeContext ctx, System.Action onComplete)
        {
            if (ctx is EventNodeTileContext tileCtx)
            {
                ExecuteTile(data, tileCtx, onComplete);
            }
            else
            {
                Debug.LogWarning(
                    $"{GetType().Name}: 需要 EventNodeTileContext，但收到 {ctx?.GetType().Name ?? "null"}，跳过执行。");
                onComplete?.Invoke();
            }
        }

        // 子类实现 Tile 专用逻辑
        public abstract void ExecuteTile(BaseNodeData data, EventNodeTileContext ctx, System.Action onComplete);
    }
}