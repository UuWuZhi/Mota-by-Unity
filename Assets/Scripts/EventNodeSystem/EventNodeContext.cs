using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 执行节点时携带的上下文信息，运行时传递到节点执行回调
/// </summary>
public class EventNodeContext
{
    /// <summary>
    /// 包含与格子/瓦片位置相关的聚合数据（CellPos, LayerId, TileObject）。
    /// 使用 EventNodeData 作为底层数据承载，方便在多个接口间传递这些信息。
    /// </summary>
    public EventNodeData Data { get; set; }


    public Vector3Int CellPos => Data?.CellPos ?? Vector3Int.zero;
    public int LayerId => Data?.LayerId ?? 0;
    public GameObject TileObject => Data?.TileObject;

    public MonoBehaviour OwnerMono; // 用于 StartCoroutine 等，通常通过 EventNodeManager 提供
    public Dictionary<string, object> Vars = new Dictionary<string, object>(); // 临时/扩展数据

    // DI 提供的容器引用，由 EventNodeManager 在构造 Context 时传入
    public GlobalServiceContainer Services { get; set; }

    // 通过 Services 获取服务
    public PlayerAttribute PlayerAttribute => Services?.PlayerAttribute;
    public GridManager GridManager => Services?.GridManager;
    public EventCenter EventCenter => Services?.EventCenter;
    public MapManager MapManager => Services?.MapManager;
    public IInventoryService InventoryService => Services?.InventoryService;

    // EventNodeManager 会在创建 Context 时将自身赋值给这里，避免循环依赖
    public EventNodeManager EventNodeManager { get; set; }

    public EventNodeContext() { }

    // 新增：通用 Get/Set 方法，用于把容器创建的单例或运行时服务通过字符串键放入上下文
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