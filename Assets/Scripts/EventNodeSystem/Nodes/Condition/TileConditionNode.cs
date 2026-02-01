using System;
using UnityEngine;

/// <summary>
/// 瓦片专用 Condition 模板：统一做类型检查并把强类型上下文交给子类 EvaluateTile 处理。
/// 保持向后兼容：子类实现 EvaluateTile 即可，无需覆写基类 Evaluate 的签名。
/// </summary>
public abstract class TileConditionNode : ConditionNode
{
    public sealed override void Evaluate(EventNodeContext ctx, Action<bool> onResult)
    {
        if (ctx is EventNodeTileContext tileCtx)
        {
            try
            {
                EvaluateTile(tileCtx, onResult);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                onResult?.Invoke(false);
            }
        }
        else
        {
            Debug.LogWarning($"{GetType().Name}: 需要 EventNodeTileContext，但收到 {ctx?.GetType().Name ?? "null"}，默认返回 false。");
            onResult?.Invoke(false);
        }
    }

    // 子类实现 Tile 专用判定逻辑
    public abstract void EvaluateTile(EventNodeTileContext ctx, Action<bool> onResult);
}
