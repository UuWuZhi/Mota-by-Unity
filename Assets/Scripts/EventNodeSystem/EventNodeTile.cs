using UnityEngine;
using VContainer;

/// <summary>
/// 事件挂载点
/// </summary>
public class EventNodeTile : MonoBehaviour
{
    public enum TriggerMode             // 事件触发方式
    {
        OnLoad,                         // 场景加载时触发
        OnPlayerEnter,                  // 玩家进入时触发
        OnPlayerArrived,                // 玩家到达时触发（进入格子后停止移动时触发）
    }
    public enum MovementControl         // 玩家移动控制方式
    {
        None,                           // 不干涉移动
        BlockPlayerDuringExecution,     // 在整个事件执行期间阻塞玩家移动（玩家已进入或已触发）
        PreventEnterUntilAllowed,       // 在玩家尝试进入时，如果事件决定不允许，则阻止进入；事件可能异步决定允许后再放行
        PreventAndBlockUntilComplete,   // 在玩家尝试进入时阻止进入，直到事件完成后再放行
    }

    public EventNode rootNode;

    public TriggerMode triggerMode = TriggerMode.OnPlayerEnter;
    public MovementControl movementControl = MovementControl.BlockPlayerDuringExecution;

    [HideInInspector] public Vector3Int CellPos;

}