using UnityEngine;

/// <summary>
/// 简单的数据载体：存储事件节点上下文中与格子/瓦片位置相关的三项信息。
/// 用于将原先散落在 EventNodeContext 中的属性抽离为单一结构。
/// </summary>
public class EventNodeData
{
    /// <summary>
    /// 格子坐标（Cell position）。
    /// </summary>
    public Vector3Int CellPos { get; set; }

    /// <summary>
    /// 当前层 Id（Layer identifier）。
    /// </summary>
    public int LayerId { get; set; }

    /// <summary>
    /// 对应的瓦片 GameObject 引用。
    /// </summary>
    public GameObject TileObject { get; set; }

    public EventNodeData() { }

    /// <summary>
    /// 构造一个包含所有字段的实例。
    /// </summary>
    public EventNodeData(Vector3Int cellPos, int layerId, GameObject tileObject)
    {
        CellPos = cellPos;
        LayerId = layerId;
        TileObject = tileObject;
    }
}