using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 格子事件触发参数（触发指定位置的事件时使用）
/// </summary>
public class GridEventTriggerEventArgs : EventArgs
{
    public Vector2 TargetWorldPos { get; set; } // 目标格子的世界坐标（中心）
    public GridType GridType { get; set; }      // 格子类型（如楼梯/怪物/道具）
    public object TriggerSource { get; set; }   // 触发来源（玩家/剧情等）
}
/// <summary>
/// 预移动请求：在玩家开始移动前发布，处理器可选择阻塞移动直到回调（例如门动画）
/// </summary>
//public class PreMoveEventArgs : EventArgs
//{
//    public GridType Type { get; set; }
//    public Vector3Int CellPos { get; set; }
//    public int LayerId { get; set; } // 新：所属楼层ID，用于多层实体定位

//    // 处理器使用字段
//    public bool Handled { get; set; } = false; // 是否有处理器处理了该事件
//    public bool AllowMoveImmediately { get; set; } = true; // 处理器是否允许立即移动（true = 允许，false = 阻塞等待 OnMoveAllowed 回调）
//    public Action OnMoveAllowed { get; set; } // 若处理器异步决定允许移动，完成时应调用这个回调
//}

/// <summary>
/// 到达后通知：玩家到达目标格子中心后发布（用于 Item / Stair 等）
/// </summary>
//public class PostMoveEventArgs : EventArgs
//{
//    public GridType Type { get; set; }
//    public Vector3Int CellPos { get; set; }
//    public int LayerId { get; set; } // 新：所属楼层ID
//}

/// <summary>
/// 新：实体动画完成事件参数，由 TileEntity 的动画事件触发
/// </summary>
public class EntityAnimationEventArgs : EventArgs
{
    public int LayerId { get; set; }
    public Vector3Int CellPos { get; set; }
    public string AnimationName { get; set; }
}

/// <summary>
/// 事件层瓦片移动事件参数
/// </summary>
public class TileMovedEventArgs : EventArgs
{
    public GridType TileType { get; set; }
    public Vector3Int FromCell { get; set; }
    public Vector3Int ToCell { get; set; }
    public int LayerId { get; set; }
    public EventTile TileAsset { get; set; } // 被移动的瓦片资源引用（可为 null）
}

/// <summary>
/// 事件层瓦片移除事件参数
/// </summary>
public class TileRemovedEventArgs : EventArgs
{
    public GridType TileType { get; set; }
    public Vector3Int Cell { get; set; }
    public int LayerId { get; set; }
    public BaseTile TileAsset { get; set; } // 被移除的瓦片资源引用（可为 null）
}