using System;
using UnityEngine;

/// <summary>
/// 条件节点：固定读取一个数值并比较，结果通过回调返回（内部同样实现 Execute）
/// 实现约定：条件节点应同时提供 Evaluate 方法供流程节点同步/异步调用
/// </summary>
public abstract class ConditionNode : EventNode
{
    /// <summary>
    /// 评估条件。异步时通过 onResult 回传结果。
    /// </summary>
    public abstract void Evaluate(EventNodeContext ctx, Action<bool> onResult);

    public override void Execute(EventNodeContext ctx, Action onComplete)
    {
        // 默认执行：评估完毕但不执行后续动作，调用 onComplete 以保持兼容
        Evaluate(ctx, result => { onComplete?.Invoke(); });
    }
}