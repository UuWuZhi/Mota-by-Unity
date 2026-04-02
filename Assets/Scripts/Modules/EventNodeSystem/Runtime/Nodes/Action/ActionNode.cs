using System;

/// <summary>
/// 动作节点：执行原子行为（不做条件判断），完成时调用 onComplete
/// </summary>
public abstract class ActionNode : EventNode
{
    public override abstract void Execute(EventNodeContext ctx, Action onComplete);
}