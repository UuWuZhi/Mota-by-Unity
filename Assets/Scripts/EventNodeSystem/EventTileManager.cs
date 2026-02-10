using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using VContainer;

public class EventTileManager : MonoBehaviour
{
    // 按层分组的 EventNodeTile 注册表：layerId -> (cellPos -> tile)
    private readonly Dictionary<int, Dictionary<Vector3Int, EventNodeTile>> _nodesByLayer =
        new Dictionary<int, Dictionary<Vector3Int, EventNodeTile>>();

    private IGlobalEventVariables _globalEventVariables;
    private GridManager _gridManager;
    private EventCenter _eventCenter;
    private IEventRunner _eventRunner;
    // 当前已注册的层（用于切换时清理或替换注册表）
    private int _currentRegisteredLayer = int.MinValue;

    private bool _eventSubscribed = false;

    //==============================================================================//
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
    public void Construct(IGlobalEventVariables globalEventVariables, GridManager gridManager, EventCenter eventCenter, IEventRunner eventRunner)
    {
        _globalEventVariables = globalEventVariables;
        _gridManager = gridManager;
        _eventCenter = eventCenter;
        _eventRunner = eventRunner;
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
    private void OnPlayerArrived_EventCenter(object sender, PlayerArrivedEventArgs args)
    {
        if (args == null) return;

        // PlayerArrivedEventArgs 中通常包含 TargetWorldPos 和 TriggerEvent 标志
        // 仅在需要触发格子事件的情况下转换并尝试触发
        try
        {
            if (args.TriggerEvent == false) return;
            Vector3Int cellPos = _gridManager.MapGrid.WorldToCell(args.TargetWorldPos);
            int layerId = _globalEventVariables.GetInt(GlobalEventKey.LayerId);
            TryTriggerEventTile(cellPos, layerId);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    // 处理：楼层切换完成事件，按层批量加载该层的 EventNodeTile 并注册
    private void OnLayerSwitched_Handler(object sender, LayerSwitchedEventArgs args)
    {
        if (args == null) return;
        try
        {
            // 从全局变量或事件参数确定当前层 id
            int layerId = _globalEventVariables != null ? _globalEventVariables.GetInt(GlobalEventKey.LayerId) : 0;

            // 清理上一次注册的层（只移除注册表，不销毁 GameObject）
            if (_currentRegisteredLayer != int.MinValue && _nodesByLayer.ContainsKey(_currentRegisteredLayer))
            {
                _nodesByLayer.Remove(_currentRegisteredLayer);
            }

            // 批量加载新层的 EventNodeTile（使用事件参数提供的 Tilemap）
            if (args.EventTilemap != null)
            {
                LoadLayerEventTiles(layerId, args.EventTilemap.gameObject);
            }
            else
            {
                Debug.LogWarning("EventTileManager: OnLayerSwitched 收到的 args.EventTilemap 为 null，无法加载事件瓦片");
            }

            _currentRegisteredLayer = layerId;

            // 触发 GridLoaded 以便处理 OnLoad 类型节点
            _eventCenter?.TriggerGridLoaded(new GridLoadedEventArgs());
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
    private void OnGridLoaded_EventCenter(object sender, GridLoadedEventArgs args)
    {
        // args 允许为 null（事件可能不包含额外信息）
        try
        {
            int layerId = _globalEventVariables.GetInt(GlobalEventKey.LayerId);
            if (!_nodesByLayer.TryGetValue(layerId, out var dict)) return;
            // 迭代时复制 keys 防止并发修改
            var keys = new List<Vector3Int>(dict.Keys);
            foreach (var cell in keys)
            {
                if (!dict.TryGetValue(cell, out var tileMono) || tileMono == null) continue;
                if (tileMono.triggerMode != EventNodeTile.TriggerMode.OnLoad) continue;
                TryTriggerEventTile(cell, layerId);
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
    private void OnEventTileMoved_Handler(object sender, TileMovedEventArgs args)
    {
        if (args == null) return;
        try
        {
            // 如果有注册的 EventNode 在源格子上，移动注册并更新 Mono 的位置
            if (args.FromCell == args.ToCell) return;
            int layerId = _globalEventVariables != null ? _globalEventVariables.GetInt(GlobalEventKey.LayerId) : 0;
            if (!_nodesByLayer.TryGetValue(layerId, out var dict)) return;
            if (!dict.TryGetValue(args.FromCell, out var node) || node == null) return;

            // 移除旧注册并注册到新格子
            dict.Remove(args.FromCell);
            dict[args.ToCell] = node;
            node.CellPos = args.ToCell;

            // 更新 gameObject 世界位置（安全检查）
            if (_gridManager?.MapGrid != null && node != null)
            {
                try
                {
                    node.transform.position = _gridManager.MapGrid.GetCellCenterWorld(args.ToCell);
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
    private void OnEventTileRemoved_Handler(object sender, TileRemovedEventArgs args)
    {
        if (args == null) return;
        try
        {
            int layerId = _globalEventVariables != null ? _globalEventVariables.GetInt(GlobalEventKey.LayerId) : 0;
            if (!_nodesByLayer.TryGetValue(layerId, out var dict)) return;
            if (!dict.TryGetValue(args.Cell, out var node) || node == null) return;
            dict.Remove(args.Cell);
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
        _eventCenter.OnPlayerArrived += OnPlayerArrived_EventCenter;
        _eventCenter.OnGridLoaded += OnGridLoaded_EventCenter;
        _eventCenter.OnEventTileMoved += OnEventTileMoved_Handler;
        _eventCenter.OnEventTileRemoved += OnEventTileRemoved_Handler;
        _eventCenter.OnLayerSwitched += OnLayerSwitched_Handler;
        _eventSubscribed = true;
    }

    /// <summary>
    /// 取消订阅事件中心的回调以避免在对象禁用或销毁后继续接收事件。
    /// </summary>
    private void UnsubscribeEventCenter()
    {
        if (_eventCenter == null || !_eventSubscribed) return;
        _eventCenter.OnPlayerArrived -= OnPlayerArrived_EventCenter;
        _eventCenter.OnGridLoaded -= OnGridLoaded_EventCenter;
        _eventCenter.OnEventTileMoved -= OnEventTileMoved_Handler;
        _eventCenter.OnEventTileRemoved -= OnEventTileRemoved_Handler;
        _eventCenter.OnLayerSwitched -= OnLayerSwitched_Handler;
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
    //                                 节点：更新                                   //
    //==============================================================================//
    // 注册/注销/查找基于格子的 EventNodeTile

    /// <summary>
    /// 在指定格子位置注册一个 EventNodeTile 实例，若该位置已有注册则覆盖。
    /// </summary>
    /// <param name="cellPos">要注册的格子坐标。</param>
    /// <param name="node">要注册的节点实例（不可为 null）。</param>
    public void RegisterEventTileAtCell(Vector3Int cellPos, EventNodeTile node)
    {
        if (node == null) return;
        int layerId = _globalEventVariables != null ? _globalEventVariables.GetInt(GlobalEventKey.LayerId) : 0;
        if (!_nodesByLayer.TryGetValue(layerId, out var dict))
        {
            dict = new Dictionary<Vector3Int, EventNodeTile>();
            _nodesByLayer[layerId] = dict;
        }
        dict[cellPos] = node;
        Debug.Log($"已在层{layerId}的{cellPos}处注册节点");
    }
    public void RegisterEventTileAtWorldPos(Vector2 worldPos, EventNodeTile node)
    {
        if (node == null) return;
        Vector3Int cellPos = _gridManager.MapGrid.WorldToCell(worldPos);
        RegisterEventTileAtCell(cellPos, node);
    }

    // 批量加载指定层的 EventNodeTile 并存入 _nodesByLayer
    public void LoadLayerEventTiles(int layerId, Tilemap eventTilemap)
    {
        if (eventTilemap == null)
        {
            Debug.LogWarning("EventTileManager.LoadLayerEventTiles: eventTilemap 为 null");
            return;
        }
        LoadLayerEventTiles(layerId, eventTilemap.gameObject);
    }

    // overload：接受层根对象（例如 Event Tilemap 的 GameObject）
    public void LoadLayerEventTiles(int layerId, GameObject layerRoot)
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

        // 准备/清理目标层字典
        if (!_nodesByLayer.TryGetValue(layerId, out var dict))
        {
            dict = new Dictionary<Vector3Int, EventNodeTile>();
            _nodesByLayer[layerId] = dict;
        }
        else
        {
            dict.Clear();
        }

        // 批量查找该层下的所有 EventNodeTile（包括未激活的）
        var tiles = layerRoot.GetComponentsInChildren<EventNodeTile>(true);
        foreach (var tile in tiles)
        {
            if (tile == null || tile.gameObject == null) continue;

            // 计算格子位置并注册
            Vector3Int cellPos;
            try
            {
                cellPos = _gridManager.MapGrid.WorldToCell(tile.transform.position);
            }
            catch
            {
                // 兜底：按世界坐标向下取整
                var p = tile.transform.position;
                cellPos = new Vector3Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y), Mathf.FloorToInt(p.z));
            }

            tile.CellPos = cellPos;
            dict[cellPos] = tile;
        }

        Debug.Log($"EventTileManager: 已为层 {layerId} 加载并注册 {dict.Count} 个 EventNodeTile");
    }

    /// <summary>
    /// 从指定格子位置注销已注册的 EventNodeTile（若存在）。
    /// </summary>
    /// <param name="cellPos">要注销的格子坐标。</param>
    public void UnregisterEventTileAtCell(Vector3Int cellPos)
    {
        int layerId = _globalEventVariables != null ? _globalEventVariables.GetInt(GlobalEventKey.LayerId) : 0;
        if (_nodesByLayer.TryGetValue(layerId, out var dict))
        {
            if (dict.ContainsKey(cellPos))
                dict.Remove(cellPos);
        }
    }
    public void UnRegisterEventTileAtWorldPos(Vector2 worldPos)
    {
        Vector3Int cellPos = _gridManager.MapGrid.WorldToCell(worldPos);
        UnregisterEventTileAtCell(cellPos);
    }
    //==============================================================================//
    //                                 节点：获取                                   //
    //==============================================================================//
    /// <summary>
    /// 尝试获取指定格子位置的 EventNodeTile 实例。
    /// </summary>
    /// <param name="cellPos">要查询的格子坐标。</param>
    /// <param name="node">输出参数，当返回 true 时包含对应的节点实例。</param>
    /// <returns>若指定格子有已注册节点且不为 null 则返回 true，否则返回 false。</returns>
    public bool TryGetEventNodeAtCell(Vector3Int cellPos, out EventNodeTile node)
    {
        int layerId = _globalEventVariables != null ? _globalEventVariables.GetInt(GlobalEventKey.LayerId) : 0;
        node = null;
        if (!_nodesByLayer.TryGetValue(layerId, out var dict)) return false;
        return dict.TryGetValue(cellPos, out node) && node != null;
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
    public bool TryTriggerEventTile(Vector3Int cellPos, int layerId)
    {
        if (!_nodesByLayer.TryGetValue(layerId, out var dict) || !dict.TryGetValue(cellPos, out var tileMono) || tileMono == null) return false;
        // 不在此处处理 OnPlayerEnter（该类型由预移动协商处理）
        if (tileMono.triggerMode == EventNodeTile.TriggerMode.OnPlayerEnter) return false;

        try
        {
            var ctx = CreateContext(cellPos, layerId, tileMono.gameObject);
            // 非阻塞触发：一般通过事件或到达触发调用，传入 null 回调（事件内部自管理完成时机）

            _eventRunner.Run(tileMono.rootNode, ctx, null);
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
        if (callback == null) throw new ArgumentNullException(nameof(callback));

        // 使用指定层的注册表查找 EventNodeTile
        if (!_nodesByLayer.TryGetValue(layerId, out var dict) || !dict.TryGetValue(cellPos, out var tileMono) || tileMono == null)
        {
            //Debug.Log("EventTileManager:请求进入格子时未找到对应的 EventNodeTile");
            callback?.Invoke(true, false);
            onExecutionComplete?.Invoke();
            return;
        }

        // 仅 OnPlayerEnter 类型在预移动阶段处理
        if (tileMono.triggerMode != EventNodeTile.TriggerMode.OnPlayerEnter)
        {
            callback?.Invoke(true, false);
            onExecutionComplete?.Invoke();
            return;
        }

        EventNodeTileContext ctx = CreateContext(cellPos, layerId, tileMono.gameObject);

        switch (tileMono.movementControl)
        {
            case EventNodeTile.MovementControl.None:
                // 不阻塞移动：立即允许进入，事件异步触发但不阻塞玩家
                try
                {
                    _eventRunner.Run(tileMono.rootNode, ctx, () => { onExecutionComplete?.Invoke(); });
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    onExecutionComplete?.Invoke();
                }
                callback?.Invoke(true, false);
                break;

            case EventNodeTile.MovementControl.BlockPlayerDuringExecution:
                // 允许进入，但阻塞玩家直到事件完成
                callback?.Invoke(true, true);
                StartCoroutine(RunAndBlockBackground(tileMono, ctx, onExecutionComplete));
                break;

            case EventNodeTile.MovementControl.PreventEnterUntilAllowed:
                // 先运行事件决定是否允许进入
                try
                {
                    _eventRunner.Run(tileMono.rootNode, ctx, () =>
                    {
                        bool allow = true;
                        if (ctx.Vars != null && ctx.Vars.TryGetValue("allowEnter", out object o) && o is bool b) allow = b;
                        callback?.Invoke(allow, false);
                        onExecutionComplete?.Invoke();
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    callback?.Invoke(false, false);
                    onExecutionComplete?.Invoke();
                }
                break;

            case EventNodeTile.MovementControl.PreventAndBlockUntilComplete:
                // 阻止进入并等待事件完成
                callback?.Invoke(false, true);
                StartCoroutine(RunAndBlockBackground(tileMono, ctx, onExecutionComplete));
                break;

            default:
                Debug.LogWarning("EventTileManager:移动方式未识别");
                callback?.Invoke(true, false);
                onExecutionComplete?.Invoke();
                break;
        }
    }

    // 在后台运行事件并阻塞玩家移动，直到事件完成
    /// <summary>
    /// 辅助协程：在后台调用节点的 Run 方法并等待其完成，然后执行完成回调。
    /// 常用于需要在事件执行期间阻塞玩家移动的场景。
    /// </summary>
    /// <param name="tile">要执行的事件节点。</param>
    /// <param name="ctx">事件上下文。</param>
    /// <param name="onExecutionComplete">事件执行完成后的回调（可为 null）。param>
    /// <returns>一个用于协程的 IEnumerator。</returns>
    private IEnumerator RunAndBlockBackground(EventNodeTile tile, EventNodeTileContext ctx, Action onExecutionComplete)
    {
        if (tile == null)
        {
            onExecutionComplete?.Invoke();
            yield break;
        }

        bool finished = false;
        try
        {
           _eventRunner.Run(tile.rootNode, ctx, () => { finished = true; });
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            finished = true;
        }
        // 等待事件执行完成
        while (!finished) yield return null;
        onExecutionComplete?.Invoke();
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
    private EventNodeTileContext CreateContext(Vector3Int cellPos, int layerId, GameObject tileObject)
    {
        var ctx = new EventNodeTileContext
        {
            Data = BuildEventNodeTileData(cellPos, layerId, tileObject),
            EventTileManager = this,
            OwnerMono = this,
        };
        return ctx;
    }
    /// <summary>
    /// 构建一个新的 <see cref="EventNodeTileData"/> 实例，包含格子坐标、层 Id 与瓦片对象。
    /// </summary>
    /// <param name="cellPos">格子坐标。</param>
    /// <param name="layerId">层 Id。</param>
    /// <param name="tileObject">瓦片对应的 GameObject 引用（可为 null）。</param>
    /// <returns>包含传入信息的 <see cref="EventNodeTileData"/> 实例。</returns>
    public EventNodeTileData BuildEventNodeTileData(Vector3Int cellPos, int layerId, GameObject tileObject)
    {
        return new EventNodeTileData(cellPos, layerId, tileObject);
    }
    #endregion
}