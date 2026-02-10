using System;
using System.Collections;
using UnityEngine;

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

    public EventRunnerService(CoroutineRunner coroutineRunner,
        GridManager gridManager,
        IInventoryService inventoryService,
        PlayerAttribute playerAttribute,
        EventCenter eventCenter,
        MapManager mapManager)
    {
        _coroutineRunner = coroutineRunner;
        _gridManager = gridManager;
        _inventoryService = inventoryService;
        _playerAttribute = playerAttribute;
        _eventCenter = eventCenter;
        _mapManager = mapManager;
    }

    public void Run(EventNode rootNode, EventNodeContext ctx, Action onComplete)
    {
        if (rootNode == null) { onComplete?.Invoke(); return; }
        ctx ??= new EventNodeContext();
        ctx.OwnerMono = _coroutineRunner;
        ctx.GridManager = _gridManager;
        ctx.InventoryService = _inventoryService;
        ctx.PlayerAttribute = _playerAttribute;
        ctx.EventCenter = _eventCenter;
        ctx.MapManager = _mapManager;

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
}
