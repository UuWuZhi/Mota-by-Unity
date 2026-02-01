using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用节点上下文基类（轻量），用于作为所有具体上下文类型的基类。
/// EventNode.Execute 接口应接受此基类以便复用不同语义的上下文（Tile/Item 等）。
/// </summary>
public class EventNodeContext
{
    public MonoBehaviour OwnerMono; // 用于 StartCoroutine 等，通常由 Runner 注入
    public Dictionary<string, object> Vars = new Dictionary<string, object>(); // 临时/扩展数据

    // DI 提供的容器引用（兼容旧代码，逐步迁移到显式注入）
    public GlobalServiceContainer Services { get; set; }

    // 方便访问常用服务（短期兼容）
    public PlayerAttribute PlayerAttribute => Services?.PlayerAttribute;
    public GridManager GridManager => Services?.GridManager;
    public EventCenter EventCenter => Services?.EventCenter;
    public MapManager MapManager => Services?.MapManager;
    public IInventoryService InventoryService => Services?.InventoryService;

    // 兼容访问 Tile 专用字段（若上下文为 EventNodeTileContext 则返回对应值，否则返回安全默认）
    public Vector3Int CellPos => this is EventNodeTileContext t ? t.CellPos : Vector3Int.zero;
    public int LayerId => this is EventNodeTileContext t2 ? t2.LayerId : 0;
    public GameObject TileObject => this is EventNodeTileContext t3 ? t3.TileObject : null;

    // EventNodeManager 会在创建 Context 时将自身赋值给这里（若适用）
    public EventNodeManager EventNodeManager { get; set; }

    public EventNodeContext() { }

    public void Set<T>(string key, T value) where T : class
    {
        if (string.IsNullOrEmpty(key)) return;
        Vars[key] = value;
    }

    public T Get<T>(string key) where T : class
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (Vars.TryGetValue(key, out var o))
        {
            return o as T;
        }
        return null;
    }
}

/// <summary>
/// 地图/瓦片专用的数据承载结构（原 EventNodeData）
/// </summary>
public class EventNodeTileData
{
    public Vector3Int CellPos { get; }
    public int LayerId { get; }
    public GameObject TileObject { get; }

    public EventNodeTileData(Vector3Int cellPos, int layerId, GameObject tileObject)
    {
        CellPos = cellPos;
        LayerId = layerId;
        TileObject = tileObject;
    }
}

/// <summary>
/// 瓦片事件专用上下文，继承自通用 EventNodeContext，包含 Tile 专用数据访问器。
/// （即原来的 EventNodeContext 的语义，现在重命名为 EventNodeTileContext）
/// </summary>
public class EventNodeTileContext : EventNodeContext
{
    public EventNodeTileData Data { get; set; }

    public Vector3Int CellPos => Data?.CellPos ?? Vector3Int.zero;
    public int LayerId => Data?.LayerId ?? 0;
    public GameObject TileObject => Data?.TileObject;

    public EventNodeTileContext() : base() { }
}
