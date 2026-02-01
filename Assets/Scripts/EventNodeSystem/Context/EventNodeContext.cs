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