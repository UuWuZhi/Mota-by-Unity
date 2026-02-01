using UnityEngine;
using VContainer;

/// <summary>
/// 事件挂载点（放在场景中代表某个事件格）
/// 增加 CellPos 字段，便于基于格子的注册/查找。
/// </summary>
public class EventNodeTile : MonoBehaviour
{
    public EventNode rootNode;

    private GridManager _gridManager;
    private EventNodeManager _eventNodeManager;
    private bool _isRegistered = false;

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
    // 现在 Run 会委托给全局的 EventNodeRunner（如果存在），以统一注入/池化行为。
    public void Run(EventNodeTileContext ctx, System.Action onComplete)
    {
        if (rootNode == null)
        {
            onComplete?.Invoke();
            return;
        }

        // 先尝试从场景中找到 Runner（通过单例注入或查找）。
        var runner = FindObjectOfType<EventNodeRunner>();
        if (runner != null)
        {
            // EventNodeRunner.Run 接受通用 EventNodeContext，因此向上转型
            runner.Run(rootNode, ctx as EventNodeContext, onComplete);
            return;
        }

        // 回退：直接执行（兼容旧逻辑）
        rootNode.Execute(ctx, onComplete);
    }

    private void TryRegister()
    {
        if (!_isRegistered && Application.isPlaying && _eventNodeManager != null && _gridManager != null)
        {
            if (_gridManager.MapGrid != null)
            {
                CellPos = _gridManager.MapGrid.WorldToCell(transform.position);
            }
            _eventNodeManager.RegisterEventNodeAtCell(CellPos, this);
            _isRegistered = true;
            //Debug.Log($"EventNodeTile 注册成功 at {CellPos}");
        }
    }

    private void OnEnable()
    {
        TryRegister();
        //Debug.Log("以上为OnEnable注册");
    }
    [Inject]
    public void Construct(GridManager gridManager, EventNodeManager eventNodeManager)
    {
        _gridManager = gridManager;
        _eventNodeManager = eventNodeManager;
        TryRegister();
        //Debug.Log("以上为Construct注册");
    }

    private void OnDisable()
    {
        if (Application.isPlaying && _eventNodeManager != null)
        {
            _eventNodeManager.UnregisterEventNodeAtCell(CellPos);
            _isRegistered = false;
            //Debug.Log($"EventNodeTile 注销成功 at {CellPos}");
        }
    }
}