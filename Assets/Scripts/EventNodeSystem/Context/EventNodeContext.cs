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

    // 兼容访问常用服务（通过 ctx.Vars 中的显式键），逐步迁移到构造器注入与强类型 ctx
    // Explicit, settable service references. Runner should set these directly.
    private PlayerAttribute _playerAttributeExplicit;
    private GridManager _gridManagerExplicit;
    private EventCenter _eventCenterExplicit;
    private MapManager _mapManagerExplicit;
    private IInventoryService _inventoryServiceExplicit;

    public PlayerAttribute PlayerAttribute
    {
        get => _playerAttributeExplicit ?? Get<PlayerAttribute>("PlayerAttribute");
        set => _playerAttributeExplicit = value;
    }

    public GridManager GridManager
    {
        get => _gridManagerExplicit ?? Get<GridManager>("GridManager");
        set => _gridManagerExplicit = value;
    }

    public EventCenter EventCenter
    {
        get => _eventCenterExplicit ?? Get<EventCenter>("EventCenter");
        set => _eventCenterExplicit = value;
    }

    public MapManager MapManager
    {
        get => _mapManagerExplicit ?? Get<MapManager>("MapManager");
        set => _mapManagerExplicit = value;
    }

    public IInventoryService InventoryService
    {
        get => _inventoryServiceExplicit ?? Get<IInventoryService>("InventoryService");
        set => _inventoryServiceExplicit = value;
    }
    // EventTileManager 会在创建 Context 时将自身赋值给这里（若适用）
    public EventTileManager EventTileManager { get; set; }

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