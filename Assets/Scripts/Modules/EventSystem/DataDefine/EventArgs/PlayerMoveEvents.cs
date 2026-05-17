using UnityEngine;

namespace Modules.EventSystem.DataDefine.EventArgs
{
    /// <summary>
    ///     玩家输入事件参数（如方向键输入）
    /// </summary>
    public class PlayerInputEventArgs : System.EventArgs
    {
        public Vector2 MoveDirection { get; set; } // 移动方向（上下左右）
        public bool IsValidInput { get; set; } // 是否为有效移动输入
    }

    /// <summary>
    ///     玩家移动完成事件参数（移动到新位置后触发）
    /// </summary>
    public class PlayerArrivedEventArgs : System.EventArgs
    {
        public bool TriggerEvent { get; set; } // 是否需要触发格子事件
        public Vector2 TargetWorldPos { get; set; } // 移动后的世界坐标
    }

    /// <summary>
    ///     玩家移动方向改变事件参数（方向键切换时触发）
    /// </summary>
    public class PlayerMoveDirectionChangedEventArgs : System.EventArgs
    {
        public float Horizontal { get; set; } // 水平方向（-1/0/1）
        public float Vertical { get; set; } // 垂直方向（-1/0/1）
    }

    /// <summary>
    ///     玩家移动状态改变事件参数（开始/停止移动时触发）
    /// </summary>
    public class PlayerMoveStateChangedEventArgs : System.EventArgs
    {
        public bool IsMoving { get; set; } // 是否正在移动
        public float MoveTime { get; set; } // 移动耗时（秒）
    }
}