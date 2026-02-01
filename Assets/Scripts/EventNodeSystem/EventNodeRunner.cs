using System;
using UnityEngine;
using VContainer;

/// <summary>
/// 统一的 EventNode 执行器（Runner）。
/// 负责注入常用服务并调用节点的 Execute 方法。
/// </summary>
public class EventNodeRunner : MonoBehaviour
{
    private GlobalServiceContainer _services;

    [Inject]
    public void Construct(GlobalServiceContainer services)
    {
        _services = services;
    }

    /// <summary>
    /// 运行指定的根节点。若 ctx 为 null，会创建一个最小 ctx（暂不池化）。
    /// onComplete 在节点完成时调用。若 rootNode 为 null，会直接立即调用 onComplete。
    /// </summary>
    public void Run(EventNode rootNode, EventNodeContext ctx, Action onComplete)
    {
        if (rootNode == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (ctx == null) ctx = new EventNodeContext();

        // 注入常用字段，避免节点直接访问 null
        ctx.Services = ctx.Services ?? _services;
        ctx.OwnerMono = ctx.OwnerMono ?? this;

        try
        {
            rootNode.Execute(ctx, onComplete);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            onComplete?.Invoke();
        }
    }
}
