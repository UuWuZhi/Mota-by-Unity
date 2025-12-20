using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 执行节点时携带的上下文信息，运行时传递到节点执行回调
/// </summary>
public class EventNodeContext
{
    public Vector3Int CellPos;
    public int LayerId;
    public GameObject TileObject; // 事件对应的GameObject
    public MonoBehaviour OwnerMono; // 用于 StartCoroutine 等，通常通过 EventNodeManager 提供
    public Dictionary<string, object> Vars = new Dictionary<string, object>(); // 临时/扩展数据

    // 兼容现有单例访问（逐步移除）
    public PlayerAttribute PlayerAttribute => PlayerAttribute.Instance;
    public GridManager GridManager => GridManager.Instance;
    public EventCenter EventCenter => EventCenter.Instance;

    // 提供基于接口的背包访问，仅返回容器/适配器提供的 InventoryAdapter.Current
    // 不再尝试回退到 PlayerInventory.Instance
    public IInventoryService InventoryService => InventoryAdapter.Current;

    public EventNodeContext() { }
}