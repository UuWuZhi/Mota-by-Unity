using System;
using System.Collections;
using System.Collections.Generic;

public interface IEventRunner
{
    void Run(EventNode rootNode, EventNodeContext ctx, Action onComplete);
    /// <summary>
    /// Run the event node and yield until completion. Returns an IEnumerator so callers
    /// (usually a MonoBehaviour) can start a coroutine to wait for completion.
    /// </summary>
    IEnumerator RunAndWait(EventNode rootNode, EventNodeContext ctx, Action onComplete);
    
    // 直接按序执行一个 EventNode 列表（非 ScriptableObject 包装）
    void RunActions(List<EventNode> actions, EventNodeContext ctx, Action onComplete);

    // 按序执行一组节点并提供 IEnumerator 以便等待完成
    IEnumerator RunActionsAndWait(List<EventNode> actions, EventNodeContext ctx, Action onComplete);
}
