using UnityEngine;
using UnityEngine.Tilemaps;

namespace Modules.EventSystem.DataDefine.EventArgs
{
    /// <summary>
    ///     楼层切换请求事件参数（由楼梯等触发，请求切换楼层）
    /// </summary>
    public class LayerSwitchRequestEventArgs : System.EventArgs
    {
        public int TargetLayerId { get; set; } // 目标楼层ID（如1-50层）
        public int SpawnPointId { get; set; } // 目标楼层的出生点ID（对应楼梯位置）
    }

    /// <summary>
    ///     楼层切换完成事件参数（地图加载完成后触发）
    /// </summary>
    public class LayerSwitchedEventArgs : System.EventArgs
    {
        public Tilemap GroundTilemap { get; set; } // 地面瓦片地图
        public Tilemap ObstacleTilemap { get; set; } // 障碍/墙瓦片地图
        public Tilemap EventTilemap { get; set; } // 事件瓦片地图（怪物/道具等）
        public BoundsInt LayerBounds { get; set; } // 楼层边界（限制移动范围）
        public Vector2 SpawnPos { get; set; } // 玩家在新楼层的出生位置
    }

    public class GridLoadedEventArgs : System.EventArgs
    {
    }
}