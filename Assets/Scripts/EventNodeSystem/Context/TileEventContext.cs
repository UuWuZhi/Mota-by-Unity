using UnityEngine;


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

