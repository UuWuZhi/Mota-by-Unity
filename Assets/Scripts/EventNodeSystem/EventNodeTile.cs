using UnityEngine;

/// <summary>
/// 事件挂载点（放在场景中代表某个事件格）
/// 增加 CellPos 字段，便于基于格子的注册/查找。
/// </summary>
public class EventNodeTile : MonoBehaviour
{
    public EventNode rootNode;

    public enum TriggerMode 
    { 
        OnLoad, 
        OnPlayerEnter,
        OnPlayerArrived,
    } 
    public TriggerMode triggerMode = TriggerMode.OnPlayerEnter;

    public enum MovementControl
    {
        None,                       // 不干涉移动
        BlockPlayerDuringExecution,  // 在整个事件执行期间阻塞玩家移动（玩家已进入或已触发）
        PreventEnterUntilAllowed,    // 在玩家尝试进入时，如果事件决定不允许，则阻止进入；事件可能异步决定允许后再放行
        PreventAndBlockUntilComplete,   // 在玩家尝试进入时阻止进入，直到事件完成后再放行
    }
    public MovementControl movementControl = MovementControl.BlockPlayerDuringExecution;

    [HideInInspector] public Vector3Int CellPos;

    // 简易接口，外部（EventNodeManager）调用以运行此 Tile 的 RootNode
    public void Run(EventNodeContext ctx, System.Action onComplete)
    {
        if (rootNode == null)
        {
            onComplete?.Invoke();
            return;
        }
        rootNode.Execute(ctx, onComplete);
    }

    private void OnEnable()
    {
        // 自动注册（仅在运行时且 GridManager & EventNodeManager 可用时）
        if (Application.isPlaying && EventNodeManager.Instance != null && GridManager.Instance != null)
        {
            if (GridManager.Instance.MapGrid != null)
            {
                CellPos = GridManager.Instance.MapGrid.WorldToCell(transform.position);
            }
            EventNodeManager.Instance.RegisterEventNodeAtCell(CellPos, this);
        }
    }

    private void OnDisable()
    {
        if (Application.isPlaying && EventNodeManager.Instance != null)
        {
            EventNodeManager.Instance.UnregisterEventNodeAtCell(CellPos);
        }
    }
}