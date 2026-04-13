using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

/// <summary>
/// Service-based event runner. Explicitly inject dependencies via constructor.
/// Uses CoroutineRunner (Mono) to run coroutines when needed.
/// Implements IEventRunner for compatibility.
/// </summary>
public class EventRunnerService : IEventRunner
{
    private readonly CoroutineRunner _coroutineRunner;
    private readonly GridManager _gridManager;
    private readonly IInventoryService _inventoryService;
    private readonly PlayerAttribute _playerAttribute;
    private readonly EventCenter _eventCenter;
    private readonly MapManager _mapManager;
    private readonly IObjectResolver _resolver;

    [Inject]
    public EventRunnerService(IObjectResolver resolver)
    {
        if (resolver == null) throw new ArgumentNullException(nameof(resolver));
        _resolver = resolver;
        _coroutineRunner = resolver.Resolve<CoroutineRunner>();
        _gridManager = resolver.Resolve<GridManager>();
        _inventoryService = resolver.Resolve<IInventoryService>();
        _playerAttribute = resolver.Resolve<PlayerAttribute>();
        _eventCenter = resolver.Resolve<EventCenter>();
        _mapManager = resolver.Resolve<MapManager>();
    }

    public void Run(EventNode rootNode, EventNodeContext ctx, Action onComplete)
    {
        if (rootNode == null) { onComplete?.Invoke(); return; }
        ctx ??= new EventNodeContext();
        ctx.OwnerMono = _coroutineRunner;

        var required = new HashSet<Type>(rootNode.GetRequiredServices() ?? Array.Empty<Type>());
        RegisterRequiredServices(required, ctx);

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

    // Optional helper to run IEnumerator with coroutine host
    public Coroutine RunCoroutine(IEnumerator routine)
    {
        if (_coroutineRunner == null) return null;
        return _coroutineRunner.Run(routine);
    }

    // Existing API: run node and provide an IEnumerator that yields until completion.
    public IEnumerator RunAndWait(EventNode rootNode, EventNodeContext ctx, Action onComplete)
    {
        if (rootNode == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        bool finished = false;
        try
        {
            Run(rootNode, ctx, () => { finished = true; onComplete?.Invoke(); });
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            finished = true;
            onComplete?.Invoke();
        }

        while (!finished) yield return null;
    }

    // New: 按序执行节点列表（非等待版）
    public void RunActions(List<EventNode> actions, EventNodeContext ctx, Action onComplete)
    {
        if (actions == null || actions.Count == 0) { onComplete?.Invoke(); return; }
        ctx ??= new EventNodeContext();
        ctx.OwnerMono = _coroutineRunner;

        var required = new HashSet<Type>();
        foreach (var node in actions)
        {
            if (node == null) continue;
            var list = node.GetRequiredServices();
            if (list == null) continue;
            foreach (var type in list)
            {
                Debug.Log($"Node {node.GetType().Name} 需要服务 {type.Name}");
                required.Add(type);
            }
        }

        RegisterRequiredServices(required, ctx, true);

        _coroutineRunner?.Run(RunActionsSequence(actions, ctx, onComplete));
    }

    // New: 按序执行节点列表并提供 IEnumerator 以便等待
    public IEnumerator RunActionsAndWait(List<EventNode> actions, EventNodeContext ctx, Action onComplete)
    {
        if (actions == null || actions.Count == 0)
        {
            onComplete?.Invoke();
            yield break;
        }

        bool finished = false;
        try
        {
            RunActions(actions, ctx, () => { finished = true; onComplete?.Invoke(); });
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            finished = true;
            onComplete?.Invoke();
        }

        while (!finished) yield return null;
    }

    // Internal coroutine to sequence through actions
    private IEnumerator RunActionsSequence(List<EventNode> actions, EventNodeContext ctx, Action onComplete)
    {
        if (actions == null) { onComplete?.Invoke(); yield break; }
        for (int i = 0; i < actions.Count; i++)
        {
            bool finished = false;
            try
            {
                actions[i]?.Execute(ctx, () => { finished = true; });
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                finished = true;
            }
            while (!finished) yield return null;
        }
        onComplete?.Invoke();
    }

    private void RegisterRequiredServices(IEnumerable<Type> required, EventNodeContext ctx, bool logRegistration = false)
    {
        if (required == null || ctx == null) return;
        foreach (var type in required)
        {
            if (TryGetKnownService(type, out var service))
            {
                if (logRegistration)
                {
                    Debug.Log($"注册服务 {type.Name} 到上下文");
                }
                ctx.RegisterService(type, service);
            }
        }
    }

    private bool TryGetKnownService(Type type, out object service)
    {
        service = null;
        if (type == null) return false;

        if (type == typeof(CoroutineRunner)) service = _coroutineRunner;
        else if (type == typeof(GridManager)) service = _gridManager;
        else if (type == typeof(IInventoryService)) service = _inventoryService;
        else if (type == typeof(PlayerAttribute)) service = _playerAttribute;
        else if (type == typeof(EventCenter)) service = _eventCenter;
        else if (type == typeof(MapManager)) service = _mapManager;
        else
        {
            // 对于非内置的硬编码类型，尝试通过 VContainer 动态解析
            try
            {
                service = _resolver.Resolve(type);
            }
            catch (Exception)
            {
                Debug.LogWarning($"EventRunnerService: 无法解析所需的服务类型 {type.Name}");
                return false;
            }
        }

        return service != null;
    }
}
