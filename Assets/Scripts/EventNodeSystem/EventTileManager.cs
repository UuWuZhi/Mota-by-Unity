using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class EventTileManager : MonoBehaviour
{
    private IEventTileRegistry _registry;
    private IGlobalEventVariables _globalEventVariables;
    private GridManager _gridManager;
    private EventCenter _eventCenter;
    private IEventRunner _eventRunner;
    private bool _eventSubscribed = false;

    //==============================================================================//
    //                                                                              //
    //                                 生命周期                                     //
    //                                                                              //
    //==============================================================================//
    #region 生命周期
    [Inject]
    /// <summary>
    /// 依赖注入构造方法，用于在对象创建时注入全局事件变量、网格管理器、事件中心、对话管理器和全局服务容器。
    /// 在注入完成后会订阅事件中心的必要事件。
    /// </summary>
    /// <param name="globalEventVariables">用于读取运行时全局事件变量的接口。</param>
    /// <param name="gridManager">负责地图网格与坐标转换的管理器。</param>
    /// <param name="eventCenter">事件中心，用于订阅与分发全局事件。</param>
    public void Construct(IGlobalEventVariables globalEventVariables, GridManager gridManager, EventCenter eventCenter, IEventRunner eventRunner, IEventTileRegistry registry)
    {
        _globalEventVariables = globalEventVariables;
        _gridManager = gridManager;
        _eventCenter = eventCenter;
        _eventRunner = eventRunner;
        _registry = registry;
        SubscribeEventCenter();
    }

    /// <summary>
    /// Unity 生命周期回调：当组件启用时尝试订阅事件中心以接收事件。
    /// </summary>
    private void OnEnable()
    {
        SubscribeEventCenter();
    }

    /// <summary>
    /// Unity 生命周期回调：当组件禁用时取消订阅事件中心以避免泄露回调。
    /// </summary>
    private void OnDisable()
    {
        UnsubscribeEventCenter();
    }
    #endregion

    //==============================================================================//
    //                                                                              //
    //                                 事件系统                                     //
    //                                                                              //
    //==============================================================================//
    #region 事件系统

    // 处理 ：玩家到达目标位置事件
    /// <summary>
    /// 事件回调：处理玩家到达目标位置事件。当事件携带触发标志时，会将世界坐标转换为格子坐标并尝试触发对应格子的事件节点。
    /// 会从全局事件变量读取当前层信息并传入触发尝试中。
    /// </summary>
    /// <param name="sender">事件发送者（通常为 EventCenter）。</param>
    /// <param name="args">包含到达目标世界位置和触发标志的事件参数。</param>
    private void OnPlayerArrived(object sender, PlayerArrivedEventArgs args)
    {
        if (args == null) return;
        // PlayerArrivedEventArgs 中通常包含 TargetWorldPos 和 TriggerEvent 标志
        // 仅在需要触发格子事件的情况下转换并尝试触发
        try
        {
            if (args.TriggerEvent == false) return;
            if (_gridManager.TryConvertWorldToCellPos(args.TargetWorldPos, out Vector3Int cellPos))
            {
                TryTriggerEventTile(cellPos);
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    // 处理：楼层切换完成事件，按层批量加载该层的 EventNodeTile 并注册
    private void OnLayerSwitched(object sender, LayerSwitchedEventArgs args)
    {
        if (args == null) return;
        try
        {
            // 批量加载新层的 EventNodeTile（使用事件参数提供的 Tilemap）
            if (args.EventTilemap != null)
            {
                LoadLayerEventTiles(args.EventTilemap.gameObject);
            }
            else
            {
                Debug.LogWarning("EventTileManager: OnLayerSwitched 收到的 args.EventTilemap 为 null，无法加载事件瓦片");
            }
            // 触发 GridLoaded 以便处理 OnLoad 类型节点
            _eventCenter.TriggerGridLoaded(new GridLoadedEventArgs());
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    //处理 ：地图格子加载完成事件
    /// <summary>
    /// 事件回调：当地图格子加载完成时触发。会遍历已注册的格子事件节点并触发所有触发模式为 OnLoad 的节点。
    /// 触发时会传入当前层 id 作为上下文信息。
    /// </summary>
    /// <param name="sender">事件发送者（通常为 EventCenter）。</param>
    /// <param name="args">与格子加载相关的事件参数（可为空）。</param>
    private void OnGridLoaded(object sender, GridLoadedEventArgs args)
    {
        // args 允许为 null（事件可能不包含额外信息）
        try
        {
            var dictView = _registry.GetLayerRegistry();
            // 迭代 keys 的快照以避免并发修改问题
            var keys = new List<Vector3Int>(dictView.Keys);
            foreach (var cell in keys)
            {
                if (!dictView.TryGetValue(cell, out var tileMono) || tileMono == null) continue;
                if (tileMono.triggerMode != EventNodeTile.TriggerMode.OnLoad) continue;
                TryTriggerEventTile(cell);
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    // 处理：事件层瓦片移动（由 GridManager 触发）
    /// <summary>
    /// 事件回调：当事件层的瓦片从一个格子移动到另一个格子时更新注册表中的记录并同步 Mono 的位置。
    /// 如果源格子存在已注册节点，则将其迁移到目标格子并更新其 CellPos 与 world 位置。
    /// </summary>
    /// <param name="sender">事件发送者（通常为 GridManager）。</param>
    /// <param name="args">包含源格子与目标格子信息的事件参数。</param>
    private void OnEventTileMoved(object sender, TileMovedEventArgs args)
    {
        if (args == null) return;
        try
        {
            // 如果有注册的 EventNode 在源格子上，移动注册并更新 Mono 的位置
            if (args.FromCell == args.ToCell) return;
            if (!_registry.TryGetEventNodeAtCell(args.FromCell, out var node) || node == null) return;

            // 移除旧注册并注册到新格子（通过 registry 接口）
            _registry.UnregisterEventTileAtCell(args.FromCell);
            _registry.RegisterEventTileAtCell(args.ToCell, node);
            node.CellPos = args.ToCell;

            // 更新 gameObject 世界位置（安全检查）
            if (_gridManager && node != null)
            {
                try
                {
                    node.transform.position = _gridManager.GetCellCenterWorld(args.ToCell);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    // 处理：事件层瓦片移除（由 GridManager 触发）
    /// <summary>
    /// 事件回调：当事件层的瓦片被移除时，从注册表中注销对应的节点并销毁其 GameObject。
    /// </summary>
    /// <param name="sender">事件发送者（通常为 GridManager）。</param>
    /// <param name="args">包含被移除格子信息的事件参数。</param>
    private void OnEventTileRemoved(object sender, TileRemovedEventArgs args)
    {
        if (args == null) return;
        try
        {
            if (!_registry.TryGetEventNodeAtCell(args.Cell, out var node) || node == null) return;
            _registry.UnregisterEventTileAtCell(args.Cell);
            if (node.gameObject != null)
                GameObject.Destroy(node.gameObject);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    /// <summary>
    /// 订阅事件中心的回调，注册所需的事件处理函数。如果已订阅或事件中心为 null 则不执行任何操作。
    /// </summary>
    private void SubscribeEventCenter()
    {
        if (_eventCenter == null || _eventSubscribed) return;
        _eventCenter.OnPlayerArrived += OnPlayerArrived;
        _eventCenter.OnGridLoaded += OnGridLoaded;
        _eventCenter.OnEventTileMoved += OnEventTileMoved;
        _eventCenter.OnEventTileRemoved += OnEventTileRemoved;
        _eventCenter.OnLayerSwitched += OnLayerSwitched;
        _eventSubscribed = true;
    }

    /// <summary>
    /// 取消订阅事件中心的回调以避免在对象禁用或销毁后继续接收事件。
    /// </summary>
    private void UnsubscribeEventCenter()
    {
        if (_eventCenter == null || !_eventSubscribed) return;
        _eventCenter.OnPlayerArrived -= OnPlayerArrived;
        _eventCenter.OnGridLoaded -= OnGridLoaded;
        _eventCenter.OnEventTileMoved -= OnEventTileMoved;
        _eventCenter.OnEventTileRemoved -= OnEventTileRemoved;
        _eventCenter.OnLayerSwitched -= OnLayerSwitched;
        _eventSubscribed = false;
    }
    #endregion

    //==============================================================================//
    //                                                                              //
    //                                 节点管理                                     //
    //                                                                              //
    //==============================================================================//
    #region 节点管理
    //==============================================================================//
    //                                 节点：加载                                   //
    //==============================================================================//
    public void LoadLayerEventTiles(GameObject layerRoot, int? layerId = null)
    {
        if (layerRoot == null)
        {
            Debug.LogWarning("EventTileManager.LoadLayerEventTiles: layerRoot 为 null");
            return;
        }

        if (_gridManager == null)
        {
            Debug.LogWarning("EventTileManager.LoadLayerEventTiles: GridManager 未注入，无法计算格子坐标");
            return;
        }

        int effectiveLayerId = ResolveLayerId(layerId);
        // 批量查找该层下的所有 EventNodeTile（包括未激活的）
        var tiles = layerRoot.GetComponentsInChildren<EventNodeTile>(true);
        foreach (var tile in tiles)
        {
            _registry.RegisterEventTileAtWorldPos(tile.transform.position, tile, effectiveLayerId);
        }

        Debug.Log($"EventTileManager: 已为层 {effectiveLayerId} 加载并注册 {_registry.GetLayerRegistry(effectiveLayerId).Count} 个 EventNodeTile");
    }
    //==============================================================================//
    //                                 节点：触发                                   //
    //==============================================================================//
    /// <summary>
    /// 尝试触发指定格子的事件节点（对外统一接入）。
    /// 返回是否找到并尝试触发（不保证事件内部逻辑的结果）。
    /// 不会触发 triggerMode 为 OnPlayerEnter 的节点（这些由预移动逻辑处理）。
    /// </summary>
    /// <param name="cellPos">目标格子坐标。</param>
    /// <param name="layerId">当前层 Id，将传入事件上下文以供事件使用。</param>
    /// <returns>如果找到可触发的节点并成功调用其 Run 方法则返回 true，否则返回 false。</returns>
    public bool TryTriggerEventTile(Vector3Int cellPos, int? layerId = null)
    {
        if (_registry == null)
        {
            Debug.LogWarning("EventTileManager.TryTriggerEventTile: IEventTileRegistry not injected");
            return false;
        }

        if (!_registry.TryGetEventNodeAtCell(cellPos, out var tileMono, layerId) || tileMono == null) return false;
        // 不在此处处理 OnPlayerEnter（该类型由预移动协商处理）
        if (tileMono.triggerMode == EventNodeTile.TriggerMode.OnPlayerEnter) return false;

        try
        {
            var ctx = CreateContext(cellPos, tileMono.gameObject, layerId);
            // 非阻塞触发：一般通过事件或到达触发调用，传入 null 回调（事件内部自管理完成时机）

            // 直接按序执行 tile 上的 actions 列表（不再使用运行时临时 ScriptableObject）
            _eventRunner?.RunActions(tileMono.actions, ctx, null);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return false;
        }
    }

    /// <summary>
    /// 预移动协商：玩家尝试进入目标格子时调用，根据格子上的 EventNodeTile 决定是否允许进入以及是否阻塞玩家移动。
    /// callback: (allowEnter, blockMovementUntilComplete)
    /// onExecutionComplete: 当后台执行完成时回调（可用于解锁玩家）。
    /// </summary>
    /// <param name="cellPos">玩家欲进入的目标格子坐标。</param>
    /// <param name="layerId">当前层 Id，传入事件上下文。</param>
    /// <param name="callback">回调函数，参数为 (allowEnter, blockMovementUntilComplete)。</param>
    /// <param name="onExecutionComplete">当事件后台执行完成时的回调（可为 null）。</param>
    public void RequestEnterCell_PreMove(Vector3Int cellPos, int layerId, Action<bool, bool> callback, Action onExecutionComplete = null)
    {
        //====================================预处理==========================================//
        // 参数检查：确保 callback 不为 null（允许 onExecutionComplete 为 null）
        if (callback == null) throw new ArgumentNullException(nameof(callback));

        // 使用指定层的注册表查找 EventNodeTile
        if (_registry == null)
        {
            Debug.LogWarning("EventTileManager.RequestEnterCell_PreMove: IEventTileRegistry未注入!");
            CompleteWithAllow(callback, onExecutionComplete);
            return;
        }

        // 如果格子上没有 EventNodeTile 或者获取失败，默认允许进入且不阻塞
        if (!_registry.TryGetEventNodeAtCell(cellPos, out EventNodeTile tileMono, layerId) || tileMono == null)
        {
            Debug.Log($"EventTileManager.RequestEnterCell_PreMove: 在格子 {cellPos} 上未找到 EventNodeTile，默认允许进入");
            CompleteWithAllow(callback, onExecutionComplete);
            return;
        }

        if (!tileMono.TryBeginTrigger())
        {
            Debug.Log($"EventTileManager.RequestEnterCell_PreMove: EventNodeTile 在格子 {cellPos} 上已被触发且未完成，拒绝进入以避免重复触发");
            CompleteWithDeny(callback, onExecutionComplete);
            return;
        }

        if (tileMono.triggerMode != EventNodeTile.TriggerMode.OnPlayerEnter)
        {
            Debug.Log($"EventTileManager.RequestEnterCell_PreMove: EventNodeTile 在格子 {cellPos} 上触发模式为 {tileMono.triggerMode}，不处理预移动请求");
            CompleteWithAllow(callback, onExecutionComplete);
            return;
        }

        // 获取基本属性并构建上下文
        EventNodeTileContext ctx = CreateContext(cellPos, tileMono.gameObject, layerId);
        var enterPermission = tileMono.enterPermission;
        var executionBlocking = tileMono.executionBlocking == EventNodeTile.ExecutionBlocking.BlockDuringExecution;

        //====================================主处理==========================================//
        try
        {
            // 首先根据 enterPermission 预设一个初始允许状态和是否需要决策的标志
            bool allow = true;
            bool needDecision = false;
            switch (enterPermission)
            {
                case EventNodeTile.EnterPermission.Allow:
                    allow = true;
                    needDecision = false;
                    break;
                case EventNodeTile.EnterPermission.Deny:
                    allow = false;
                    needDecision = false;
                    break;
                case EventNodeTile.EnterPermission.DecideAfterExecution:
                    allow = false; 
                    needDecision = true;
                    // 如果 enterPermission 是 DecideAfterExecution，则必须阻塞以等待事件执行完成后获取决策结果。
                    executionBlocking = true;
                    break;
                default:
                    Debug.LogWarning("EventTileManager.RequestEnterCell_PreMove: 未识别的 EnterPermission");
                    tileMono.EndTrigger();
                    allow = true;
                    needDecision = false;
                    break;
            }

            // 然后根据 executionBlocking 决定触发事件的方式
            if (executionBlocking)
            {
                // 阻塞触发处理：先通知调用者当前允许状态和即将阻塞的事实，然后执行事件并等待完成后根据上下文变量更新允许状态并通知调用者决策结果
                callback?.Invoke(allow, true);
                if (_eventRunner == null)
                {
                    Debug.LogError("EventTileManager.RequestEnterCell_PreMove: IEventRunner 未注入，无法阻塞执行");
                    tileMono.EndTrigger();
                    callback?.Invoke(allow, false);
                    onExecutionComplete?.Invoke();
                }
                else
                {
                    StartCoroutine(_eventRunner.RunActionsAndWait(tileMono.actions, ctx, () =>
                    {
                        if (needDecision)
                        {
                            bool finalAllow = true;
                            try
                            {
                                if (ctx.Vars != null && ctx.Vars.TryGetValue("allowEnter", out object o) && o is bool b) finalAllow = b;
                            }
                            catch (Exception ex)
                            {
                                Debug.LogException(ex);
                                finalAllow = false;
                            }
                            // notify caller of final decision; movement is no longer blocked
                            callback?.Invoke(finalAllow, false);
                        }
                        tileMono.EndTrigger();
                        onExecutionComplete?.Invoke();
                    }));
                }
            }
            else
            {
                // 非阻塞触发处理：直接触发事件（不等待完成），允许状态由预设的 enterPermission 决定，执行完成后仅通知调用者事件已完成（不传递决策结果）
                    try
                    {
                        _eventRunner?.RunActions(tileMono.actions, ctx, () => { tileMono.EndTrigger(); onExecutionComplete?.Invoke(); });
                    }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    onExecutionComplete?.Invoke();
                }
                callback?.Invoke(allow, false);
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            CompleteWithAllow(callback, onExecutionComplete);
        }
    }
    #endregion
    //==============================================================================//
    //                                                                              //
    //                                 辅助方法                                     //
    //                                                                              //
    //==============================================================================//
    #region 辅助方法
    /// <summary>
    /// 构建节点Context
    /// </summary>
    /// <param name="cellPos"></param>
    /// <param name="layerId"></param>
    /// <param name="tileObject"></param>
    /// <returns></returns>
    private EventNodeTileContext CreateContext(Vector3Int cellPos, GameObject tileObject, int? layerId = null)
    {
        var ctx = new EventNodeTileContext
        {
            Data = BuildEventNodeTileData(cellPos, tileObject, layerId),
        };
            ctx.RegisterService<EventTileManager>(this);
        return ctx;
    }
    /// <summary>
    /// 构建一个新的 <see cref="EventNodeTileData"/> 实例，包含格子坐标、层 Id 与瓦片对象。
    /// </summary>
    /// <param name="cellPos">格子坐标。</param>
    /// <param name="layerId">层 Id。</param>
    /// <param name="tileObject">瓦片对应的 GameObject 引用（可为 null）。</param>
    /// <returns>包含传入信息的 <see cref="EventNodeTileData"/> 实例。</returns>
    public EventNodeTileData BuildEventNodeTileData(Vector3Int cellPos, GameObject tileObject, int? layerId = null)
    {
        int effectiveLayerId = ResolveLayerId(layerId);
        return new EventNodeTileData(cellPos, effectiveLayerId, tileObject);
    }

    /// <summary>
    /// 解析传入的可选 layerId，若为空则回退到全局事件变量（若未注入则返回 0）。
    /// </summary>
    private int ResolveLayerId(int? layerId = null)
    {
        if (layerId.HasValue) return layerId.Value;
        return _globalEventVariables != null ? _globalEventVariables.GetInt(GlobalEventKey.LayerId) : 0;
    }

    /// <summary>
    /// 统一处理异常场景下的回调，默认允许进入并结束执行。
    /// </summary>
    private void CompleteWithAllow(Action<bool, bool> callback, Action onExecutionComplete)
    {
        callback?.Invoke(true, false);
        onExecutionComplete?.Invoke();
    }
    private void CompleteWithDeny(Action<bool, bool> callback, Action onExecutionComplete)
    {
        callback?.Invoke(false, false);
        onExecutionComplete?.Invoke();
    }
    #endregion
}