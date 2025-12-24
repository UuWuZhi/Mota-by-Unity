//EventTile管理器：单例，负责注册/查找基于格子的 EventNodeTile，并响应事件中心的触发请求。
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class EventNodeManager : MonoBehaviour
{
    public static EventNodeManager Instance { get; private set; }
    // 基于格子的 EventNodeTile 注册表
    private readonly Dictionary<Vector3Int, EventNodeTile> _nodesByCell = new Dictionary<Vector3Int, EventNodeTile>();

    private IInventoryService _inventoryService;
    //==============================================================================//
    //                                                                              //
    //                                 生命周期                                     //
    //                                                                              //
    //==============================================================================//
    #region 生命周期
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Constructor-style injection for MonoBehaviour: VContainer will call this after injection
    [Inject]
    public void Construct(IInventoryService inventory)
    {
        _inventoryService = inventory; // may be null if not registered; callers should handle null
    }

    private void OnEnable()
    {
        // 订阅 EventCenter 事件（若可用）
        if (EventCenter.Instance != null)
        {
            EventCenter.Instance.OnPlayerArrived += OnPlayerArrived_EventCenter;
            EventCenter.Instance.OnGridLoaded += OnGridLoaded_EventCenter;
        }
    }

    private void OnDisable()
    {
        if (EventCenter.Instance != null)
        {
            EventCenter.Instance.OnPlayerArrived -= OnPlayerArrived_EventCenter;
            EventCenter.Instance.OnGridLoaded -= OnGridLoaded_EventCenter;
        }
    }
    #endregion
    //==============================================================================//
    //                                                                              //
    //                                 事件系统                                     //
    //                                                                              //
    //==============================================================================//
    #region 事件系统
    // 处理 ：玩家到达目标位置事件
    private void OnPlayerArrived_EventCenter(object sender, PlayerArrivedEventArgs args)
    {
        if (args == null) return;

        // PlayerArrivedEventArgs 中通常包含 TargetWorldPos 和 TriggerEvent 标志
        // 仅在需要触发格子事件的情况下转换并尝试触发
        try
        {
            if (args.TriggerEvent == false) return;
            Vector3Int cellPos = GridManager.Instance.MapGrid.WorldToCell(args.TargetWorldPos);
            int layerId = GlobalEventVariables.Instance.LayerId;
            TryTriggerEventTile(cellPos, layerId);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }
    //处理 ：地图格子加载完成事件
    private void OnGridLoaded_EventCenter(object sender, GridLoadedEventArgs args)
    {
        if (args == null) return;

        try
        {
            // 触发所有注册且 triggerMode == OnLoad 的节点（传入 layerId）
            int layerId = GlobalEventVariables.Instance.LayerId;

            // 迭代时复制 keys 防止并发修改
            var keys = new List<Vector3Int>(_nodesByCell.Keys);
            foreach (var cell in keys)
            {
                if (!_nodesByCell.TryGetValue(cell, out var tileMono) || tileMono == null) continue;
                if (tileMono.triggerMode != EventNodeTile.TriggerMode.OnLoad) continue;
                TryTriggerEventTile(cell, layerId);
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
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
    public void RegisterEventNodeAtCell(Vector3Int cellPos, EventNodeTile node)
    {
        if (node == null) return;
        _nodesByCell[cellPos] = node;
    }

    public void UnregisterEventNodeAtCell(Vector3Int cellPos)
    {
        if (_nodesByCell.ContainsKey(cellPos))
            _nodesByCell.Remove(cellPos);
    }
    //==============================================================================//
    //                                 节点：获取                                   //
    //==============================================================================//
    public bool TryGetEventNodeAtCell(Vector3Int cellPos, out EventNodeTile node)
    {
        return _nodesByCell.TryGetValue(cellPos, out node);
    }
    //==============================================================================//
    //                                 节点：触发                                   //
    //==============================================================================//
    /// <summary>
    /// 统一尝试触发指定格子的事件节点（对外统一接入：cellPos + layerId）
    /// 返回：是否找到并尝试触发（不表示事件逻辑内部的结果）
    /// - 不会触发 triggerMode == OnPlayerEnter 的节点（这些由预移动逻辑 RequestEnterCell_PreMove 处理）
    /// - 调用方可通过 layerId 参数把当前层信息传入（供 ctx 使用）
    /// </summary>
    public bool TryTriggerEventTile(Vector3Int cellPos, int layerId)
    {
        if (Instance == null) return false;
        if (!_nodesByCell.TryGetValue(cellPos, out var tileMono) || tileMono == null) return false;
        // 不在此处处理 OnPlayerEnter（该类型由预移动协商处理）
        if (tileMono.triggerMode == EventNodeTile.TriggerMode.OnPlayerEnter) return false;

        try
        {
            var ctx = new EventNodeContext
            {
                CellPos = cellPos,
                LayerId = layerId,
                TileObject = tileMono.gameObject,
                OwnerMono = this,
                InventoryService = _inventoryService
            };
            // 非阻塞触发：一般通过事件或到达触发调用，传入 null 回调（事件内部自管理完成时机）
            tileMono.Run(ctx, null);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return false;
        }
    }

    /// <summary>
    /// 预移动协商：玩家尝试进入目标格子时调用。
    /// callback: (allowEnter, blockMovementUntilComplete)
    /// onExecutionComplete: 当后台执行完成时回调（可用于解锁玩家）
    /// </summary>
    public void RequestEnterCell_PreMove(Vector3Int cellPos, int layerId, Action<bool, bool> callback, Action onExecutionComplete = null)
    {
        //Debug.Log("接收到PreMove");
        if (Instance == null)
        {
            callback?.Invoke(true, false);
            onExecutionComplete?.Invoke();
            return;
        }
        //Debug.Log("开始查找EventNodeTile");
        // 优先使用基于格子的注册表查找 EventNodeTile —— 避免依赖物理 Overlap
        if (!TryGetEventNodeAtCell(cellPos, out var tileMono) || tileMono == null)
        {
            callback?.Invoke(true, false);
            onExecutionComplete?.Invoke();
            return;
        }
        //Debug.Log("识别到EventNodeTile");
        if (tileMono.triggerMode != EventNodeTile.TriggerMode.OnPlayerEnter)
        {
            // 非进入触发类型，直接允许通过
            callback?.Invoke(true, false);
            onExecutionComplete?.Invoke();
            return;
        }
        var ctx = new EventNodeContext { CellPos = cellPos, LayerId = layerId, TileObject = tileMono.gameObject, OwnerMono = this, InventoryService = _inventoryService };

        switch (tileMono.movementControl)
        {
            case EventNodeTile.MovementControl.None:
                //Debug.Log("移动方式：通行");
                // 不阻塞移动：立即允许进入，事件异步触发但不阻塞玩家
                try
                {
                    tileMono.Run(ctx, () => { onExecutionComplete?.Invoke(); });
                }
                catch (Exception ex) { Debug.LogException(ex); onExecutionComplete?.Invoke(); }
                callback?.Invoke(true, false);
                break;

            case EventNodeTile.MovementControl.BlockPlayerDuringExecution:
                //Debug.Log("移动方式：阻塞");
                // 允许进入，但阻塞玩家直到事件完成：回调立即返回 allow=true, block=true；事件在后台执行，完成时触发 onExecutionComplete。
                callback?.Invoke(true, true);
                StartCoroutine(RunAndBlockBackground(tileMono, ctx, onExecutionComplete));
                break;

            case EventNodeTile.MovementControl.PreventEnterUntilAllowed:
                //Debug.Log("移动方式：判断");
                // 先运行事件决定是否允许进入（event 将在 ctx.Vars["allowEnter"] 写入结果或直接控制 logic）
                try
                {
                    tileMono.Run(ctx, () =>
                    {
                        bool allow = true;
                        if (ctx.Vars.TryGetValue("allowEnter", out object o) && o is bool b) allow = b;
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
                //Debug.Log("移动方式：阻止并等待");
                callback?.Invoke(false, true);
                StartCoroutine(RunAndBlockBackground(tileMono, ctx, onExecutionComplete));
                break;

            default:
                Debug.Log("移动方式：未识别");
                callback?.Invoke(true, false);
                onExecutionComplete?.Invoke();
                break;
        }
    }
    // 在后台运行事件并阻塞玩家移动，直到事件完成
    private IEnumerator RunAndBlockBackground(EventNodeTile tile, EventNodeContext ctx, Action onExecutionComplete)
    {
        bool finished = false;
        try
        {
            tile.Run(ctx, () => { finished = true; });
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
}