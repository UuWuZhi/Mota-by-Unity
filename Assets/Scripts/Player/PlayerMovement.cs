using System;
using System.Collections;
using UnityEngine;
using VContainer;

public class PlayerMovement : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f;
    [Header("组件引用")]
    [SerializeField] private Rigidbody2D rb;


    private Vector2 moveDir;
    private Coroutine moveCoroutine;
    private Vector2 targetWorldPos;

    private IGlobalEventVariables _globalEventVariables;
    private EventTileManager _eventNodeManager;
    private GridManager _gridManager;
    private EventCenter _eventCenter;

    private bool _eventSubscribed = false;

    public event EventHandler<PlayerInputEventArgs> OnMoveInput;
    public event EventHandler<PlayerMoveDirectionChangedEventArgs> OnMoveDirectionChanged;
    public event EventHandler<PlayerMoveStateChangedEventArgs> OnMoveStateChanged;
    private bool _isMoving = false;
    private bool _waitingForEventExecution = false;
    //==============================================================================//
    //                                                                              //
    //                                 生命周期                                     //
    //                                                                              //
    //==============================================================================//
    #region 生命周期
    private void OnEnable()
    {
        SubscribeEventCenter();
    }
    [Inject]
    public void Construct(IGlobalEventVariables globalEventVariables, EventCenter eventCenter, GridManager gridManager, EventTileManager eventNodeManager)
    {
        _globalEventVariables = globalEventVariables;
        _eventCenter = eventCenter;
        _gridManager = gridManager;
        _eventNodeManager = eventNodeManager;
        SubscribeEventCenter();
    }

    private void OnDisable()
    {
        UnsubscribeEventCenter();
    }
    #endregion
    //==============================================================================//
    //                                                                              //
    //                                 事件系统                                     //
    //                                                                              //
    //==============================================================================//
    #region 事件系统
    private void OnLayerSwitched(object sender, LayerSwitchedEventArgs args)
    {
        TeleportToPosition(args.SpawnPos, triggerEvent: false);
        Debug.Log($"玩家已移动到新楼层出生点：{args.SpawnPos}");
    }

    // 接收输入事件
    public void HandleMoveInput(PlayerInputEventArgs args)
    {
        OnMoveInput?.Invoke(this, args);
        // 若我们正在等待事件执行完成（被阻塞），忽略新的输入
        if (_waitingForEventExecution)
            return;
        // 如果正在对话，阻止移动输入
        if (_globalEventVariables.GetBool(GlobalEventKey.DialogueIsActive))
            return;
        // 移动中/输入无效/无方向 → 直接跳过
        if (_isMoving || !args.IsValidInput || args.MoveDirection == Vector2.zero)
        {
            return;
        }

        // 1. 拿到事件传递的移动方向（替代原Update中的输入获取）
        Vector2 dir = args.MoveDirection;
        float horizontal = dir.x;
        float vertical = dir.y;
        // 2. 同步Blender混合树的horizontal/vertical参数
        OnMoveDirectionChanged?.Invoke(this, new PlayerMoveDirectionChangedEventArgs
        {
            Horizontal = horizontal,
            Vertical = vertical
        });
        // 3. 计算目标位置（引用GridManager的tileSize）
        Vector3Int targetCell = ComputeTargetCellAndWorldPos(dir);

        // 新逻辑：
        // 1) 先判断是否在边界内（在边界内才可通行）
        if (!_gridManager.IsInGridBounds(targetCell))
        {
            // 超出边界 -> 阻止移动并触发到达事件（不触发事件逻辑）
            NotifyBlockedMovement(targetWorldPos);
            return;
        }

        // 2) 再判断基础层能否通行：先检查 Ground 层是否有地面（若无地面则不可通行）
        var groundTile = _gridManager.GetGroundTileAtWorldPos(targetWorldPos);
        if (groundTile == null)
        {
            // Ground 层无地面 -> 阻止移动
            NotifyBlockedMovement(targetWorldPos);
            return;
        }

        // 再检查 Obstacle 层是否存在阻挡（若有则不可通行）
        var obstacleTile = _gridManager.GetObstacleTileAtWorldPos(targetWorldPos);
        if (obstacleTile != null)
        {
            NotifyBlockedMovement(targetWorldPos);
            return;
        }

        // 3) 最后委托给 EventTileManager 决定是否允许进入以及是否在事件执行期间阻塞玩家
        _eventNodeManager.RequestEnterCell_PreMove(targetCell, _globalEventVariables.GetInt(GlobalEventKey.LayerId),
            (allowEnter, blockUntilComplete) =>
            {
                if (!allowEnter)
                {
                    // 阻止移动
                    NotifyBlockedMovement(targetWorldPos);
                    return;
                }

                // 允许进入
                StartMoveProcess(targetWorldPos, targetCell, blockUntilComplete, () =>
                {
                    // 当事件执行决定阻塞玩家直到完成时，EventTileManager 会在后台执行并在完成时调用此回调
                    // 该回调用于解锁玩家输入。
                    _waitingForEventExecution = false;
                });
            },
            // onExecutionComplete: 当 EventTileManager 在后台执行完成时会调用此回调（仅在需要时）
            null
        );
    }
    private void SubscribeEventCenter()
    {
        if (_eventCenter == null || _eventSubscribed) return;
        _eventCenter.OnLayerSwitched += OnLayerSwitched;
        _eventSubscribed = true;
    }
    private void UnsubscribeEventCenter()
    {
        if (_eventCenter == null || !_eventSubscribed) return;
        _eventCenter.OnLayerSwitched -= OnLayerSwitched;
        _eventSubscribed = false;
    }

    // 统一处理无法移动时的事件发布
    private void NotifyBlockedMovement(Vector2 blockedTargetWorldPos)
    {
        OnMoveStateChanged?.Invoke(this, new PlayerMoveStateChangedEventArgs { IsMoving = false, MoveTime = 0 });
        _eventCenter.TriggerPlayerArrived(new PlayerArrivedEventArgs { TriggerEvent = false, TargetWorldPos = blockedTargetWorldPos });
    }
    #endregion
    //==============================================================================//
    //                                                                              //
    //                                 玩家移动                                     //
    //                                                                              //
    //==============================================================================//
    #region 玩家移动
    private void StartMoveProcess(Vector2 targetPos, Vector3Int cellPos, bool blockUntilComplete, System.Action onExecutionComplete)
    {
        float moveTime = _gridManager.tileSize / moveSpeed;
        OnMoveStateChanged?.Invoke(this, new PlayerMoveStateChangedEventArgs { IsMoving = true, MoveTime = moveTime });

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }

        // 如果事件要求阻塞玩家直到执行完成，则设置标志（等待 EventTileManager 在完成时触发解锁回调）
        if (blockUntilComplete)
        {
            _waitingForEventExecution = true;
        }

        moveCoroutine = StartCoroutine(MoveToTarget(targetPos, () =>
        {
            // 到达后触发玩家移动事件（由订阅方决定是否响应）
            _eventCenter.TriggerPlayerArrived(new PlayerArrivedEventArgs { TriggerEvent = true, TargetWorldPos = targetPos });
            // 如果事件不要求阻塞，则立即解锁（若要求阻塞则会由 EventTileManager 在事件完成时调用 onExecutionComplete）
            if (!blockUntilComplete)
            {
                _waitingForEventExecution = false;
                onExecutionComplete?.Invoke();
            }

            // 如果 blockUntilComplete 且 onExecutionComplete 非 null，则保持等待（由外部回调解除）
            moveCoroutine = null;
        }));
    }
    private IEnumerator MoveToTarget(Vector2 targetPos, System.Action onComplete = null)
    {
        _isMoving = true;
        // 物理帧驱动 → 移动更稳定，和动画同步
        while (Vector2.SqrMagnitude((Vector2)transform.position - targetPos) > 0.000001f)
        {
            rb.MovePosition(Vector2.MoveTowards(transform.position, targetPos, moveSpeed * Time.fixedDeltaTime));
            yield return new WaitForFixedUpdate();
        }

        // 精准到位 + 重置动画状态
        transform.position = targetPos;
        rb.position = targetPos;
        _isMoving = false;
        OnMoveStateChanged?.Invoke(this, new PlayerMoveStateChangedEventArgs
        {
            IsMoving = false,
            MoveTime = 0
        });

        onComplete?.Invoke();
    }

    // 直接传送玩家到指定位置
    public void TeleportToPosition(Vector2 targetPos, bool triggerEvent = true)
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        _isMoving = false;
        _waitingForEventExecution = false;
        OnMoveStateChanged?.Invoke(this, new PlayerMoveStateChangedEventArgs
        {
            IsMoving = false,
            MoveTime = 0
        });

        // 直接设置位置（同时更新刚体位置避免物理问题）
        transform.position = targetPos;
        rb.position = targetPos;

        _eventCenter.TriggerPlayerArrived(new PlayerArrivedEventArgs
        {
            TriggerEvent = triggerEvent,
            TargetWorldPos = targetWorldPos
        });
        Debug.Log($"玩家已传送至: {targetPos}");
    }
    #endregion
    //==============================================================================//
    //                                                                              //
    //                                 辅助函数                                     //
    //                                                                              //
    //==============================================================================//
    #region 辅助函数
    // 计算目标格子与世界坐标的辅助函数
    private Vector3Int ComputeTargetCellAndWorldPos(Vector2 dir)
    {
        Vector2 currentCenter = (Vector2)transform.position + new Vector2(0.5f, 0.5f);
        Vector2 targetCenter = currentCenter + dir * _gridManager.tileSize;
        targetWorldPos = targetCenter - new Vector2(0.5f, 0.5f);
        return _gridManager.MapGrid.WorldToCell(targetWorldPos);
    }
    #endregion
}