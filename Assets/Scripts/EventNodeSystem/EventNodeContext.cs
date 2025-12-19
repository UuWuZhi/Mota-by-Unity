using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 执行上下文：携带格子信息、临时变量、运行时引用等
/// </summary>
public class EventNodeContext
{
    public Vector3Int CellPos;
    public int LayerId;
    public GameObject TileObject; // 如果有对应物体可传入
    public MonoBehaviour OwnerMono; // 用于 StartCoroutine 等（通常传 EventNodeManager.Instance）
    public Dictionary<string, object> Vars = new Dictionary<string, object>(); // 临时/局部变量共享

    // 便捷访问：可扩展为访问玩家、背包等单例引用
    public PlayerAttribute PlayerAttribute => PlayerAttribute.Instance;
    public GridManager GridManager => GridManager.Instance;
    public PlayerInventory PlayerInventory => PlayerInventory.Instance;
    public EventCenter EventCenter => EventCenter.Instance;
    public EventNodeContext() { }
}