using System;
using UnityEngine;

/// <summary>
/// 基类：所有节点都继承此类（ScriptableObject，便于可视化编辑）
/// Execute: 执行节点逻辑并在完成时调用 onComplete（可异步）
/// </summary>
public abstract class EventNode : ScriptableObject
{
    public string nodeName = "EventNode";

    /// <summary>
    /// 执行节点。完成后必须调用 onComplete (可以在下一帧或动画结束后)
    /// </summary>
    public abstract void Execute(EventNodeContext ctx, Action onComplete);

    /// <summary>
    /// 声明该节点在执行时静态需要哪些服务（类型列表）。
    /// 默认不需要任何额外服务。子类可覆盖返回需要的类型，例如
    /// return new[] { typeof(DialogueManager), typeof(IInventoryService) };
    /// </summary>
    public virtual Type[] GetRequiredServices()
    {
        return Array.Empty<Type>();
    }
}