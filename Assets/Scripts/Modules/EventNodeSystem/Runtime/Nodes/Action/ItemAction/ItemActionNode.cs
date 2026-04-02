using System;
using UnityEngine;

/// <summary>
/// 针对 Item 的 ActionNode 模板：统一做类型判断并把强类型 ctx 交给 ExecuteItem 实现者处理。
/// 禁止子类直接重写基类 Execute，以保证类型检查统一。
/// </summary>
public abstract class ItemActionNode : ActionNode
{
    // 禁止子类直接重写基类 Execute，以保证类型检查统一
    public sealed override void Execute(EventNodeContext ctx, Action onComplete)
    {
        if (ctx is ItemEventContext itemCtx)
        {
            ExecuteItem(itemCtx, onComplete);
        }
        else
        {
            Debug.LogWarning($"{GetType().Name}: 需要 ItemEventContext，但收到 {ctx?.GetType().Name ?? "null"}，跳过执行。");
            onComplete?.Invoke();
        }
    }

    // 子类实现 Item 专用逻辑
    public abstract void ExecuteItem(ItemEventContext ctx, Action onComplete);
}
