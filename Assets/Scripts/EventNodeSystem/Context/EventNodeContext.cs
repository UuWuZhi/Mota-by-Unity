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

    public PlayerAttribute PlayerAttribute { get; set; }
    public GridManager GridManager { get; set; }
    public EventCenter EventCenter { get; set; }
    public MapManager MapManager { get; set; }
    public IInventoryService InventoryService { get; set; }
    public DialogueManager DialogueManager { get; set; }
    public EventTileManager EventTileManager { get; set; }

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