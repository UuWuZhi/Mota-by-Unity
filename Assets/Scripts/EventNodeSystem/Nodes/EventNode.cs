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
}