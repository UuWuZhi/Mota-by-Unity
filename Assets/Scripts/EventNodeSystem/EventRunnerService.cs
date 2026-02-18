using System;
using System.Collections;
using System.Collections.Generic;
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
    private readonly DialogueManager _dialogueManager;

    public EventRunnerService(
        CoroutineRunner coroutineRunner,
        GridManager gridManager,
        IInventoryService inventoryService,
        PlayerAttribute playerAttribute,
        EventCenter eventCenter,
        MapManager mapManager,
        DialogueManager dialogueManager)
    {
        _coroutineRunner = coroutineRunner;
        _gridManager = gridManager;
        _inventoryService = inventoryService;
        _playerAttribute = playerAttribute;
        _eventCenter = eventCenter;
        _mapManager = mapManager;
        _dialogueManager = dialogueManager;
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
        ctx.DialogueManager = _dialogueManager;

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
        ctx.GridManager = _gridManager;
        ctx.InventoryService = _inventoryService;
        ctx.PlayerAttribute = _playerAttribute;
        ctx.EventCenter = _eventCenter;
        ctx.MapManager = _mapManager;
        ctx.DialogueManager = _dialogueManager;

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
}
