using UnityEngine;

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