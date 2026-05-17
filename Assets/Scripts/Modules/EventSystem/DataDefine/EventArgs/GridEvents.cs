using Modules.Map.DataDefine;
using Modules.Map.DataDefine.Tile;
using UnityEngine;

namespace Modules.EventSystem.DataDefine.EventArgs
{
    /// <summary>
    ///     格子事件触发参数（触发指定位置的事件时使用）
    /// </summary>
    public class GridEventTriggerEventArgs : System.EventArgs
    {
        public Vector2 TargetWorldPos { get; set; } // 目标格子的世界坐标（中心）
        public GridType GridType { get; set; } // 格子类型（如楼梯/怪物/道具）
        public object TriggerSource { get; set; } // 触发来源（玩家/剧情等）
    }

    /// <summary>
    ///     新：实体动画完成事件参数，由 TileEntity 的动画事件触发
    /// </summary>
    public class EntityAnimationEventArgs : System.EventArgs
    {
        public int LayerId { get; set; }
        public Vector3Int CellPos { get; set; }
        public string AnimationName { get; set; }
    }

    /// <summary>
    ///     事件层瓦片移动事件参数
    /// </summary>
    public class TileMovedEventArgs : System.EventArgs
    {
        public GridType TileType { get; set; }
        public Vector3Int FromCell { get; set; }
        public Vector3Int ToCell { get; set; }
        public int LayerId { get; set; }
        public EventTile TileAsset { get; set; } // 被移动的瓦片资源引用（可为 null）
    }

    /// <summary>
    ///     事件层瓦片移除事件参数
    /// </summary>
    public class TileRemovedEventArgs : System.EventArgs
    {
        public GridType TileType { get; set; }
        public Vector3Int Cell { get; set; }
        public int LayerId { get; set; }
        public BaseTile TileAsset { get; set; } // 被移除的瓦片资源引用（可为 null）
    }
}