using System;
using System.Collections;
using System.Collections.Generic;
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
    private Queue<Vector3Int> _pathQueue;
    private Vector3Int? _pathTargetCell;
    private int _pathMoveToken = 0;
    private int _moveToken = 0;

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
    private bool _isPathMoving = false;
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
        if (_isPathMoving)
        {
            CancelPathMove();
        }
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
        int token = ++_moveToken;
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

        moveCoroutine = StartCoroutine(MoveToTarget(targetPos, token, () =>
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
    private IEnumerator MoveToTarget(Vector2 targetPos, int token, System.Action onComplete = null)
    {
        _isMoving = true;
        // 物理帧驱动 → 移动更稳定，和动画同步
        while (token == _moveToken && Vector2.SqrMagnitude((Vector2)transform.position - targetPos) > 0.000001f)
        {
            rb.MovePosition(Vector2.MoveTowards(transform.position, targetPos, moveSpeed * Time.fixedDeltaTime));
            yield return new WaitForFixedUpdate();
        }

        if (token != _moveToken)
        {
            yield break;
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

    // 直接传送玩家到指定位置（暂未使用）
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

        targetWorldPos = targetPos;

        _eventCenter.TriggerPlayerArrived(new PlayerArrivedEventArgs
        {
            TriggerEvent = triggerEvent,
            TargetWorldPos = targetPos
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
    /// <summary>
    /// 尝试通过鼠标点击发起逐步移动（世界坐标入口）。
    /// </summary>
    /// <param name="worldPos">鼠标点击的世界坐标。</param>
    /// <returns>是否成功发起逐步移动。</returns>
    public bool TryMoveToWorldPosStep(Vector2 worldPos)
    {
        if (_gridManager == null || _globalEventVariables == null) return false;
        if (_gridManager.TryConvertWorldToCellPos(worldPos, out Vector3Int targetCell))
        {
            return TryMoveToCellStep(targetCell);
        }
        return false;
    }

    /// <summary>
    /// 尝试通过鼠标点击发起逐步移动（格子坐标入口）。
    /// </summary>
    /// <param name="targetCell">目标格子坐标。</param>
    /// <returns>是否成功发起逐步移动。</returns>
    private bool TryMoveToCellStep(Vector3Int targetCell)
    {
        if (_isPathMoving || _isMoving)
        {
            CancelPathMove();
        }
        if (_waitingForEventExecution) return false;
        if (_globalEventVariables.GetBool(GlobalEventKey.DialogueIsActive)) return false;
        if (_gridManager == null || !_gridManager.IsInGridBounds(targetCell)) return false;

        Vector3Int startCell = _gridManager.MapGrid.WorldToCell(transform.position);
        if (startCell == targetCell) return false;
        var path = BuildPath(startCell, targetCell);
        if (path == null || path.Count == 0) return false;

        StartPathMove(path, targetCell);
        return true;
    }

    /// <summary>
    /// 启动路径移动流程，并缓存路径队列。
    /// </summary>
    /// <param name="path">按顺序的路径格子列表（不含起点）。</param>
    /// <param name="targetCell">点击的目标格子。</param>
    private void StartPathMove(List<Vector3Int> path, Vector3Int targetCell)
    {
        _isPathMoving = true;
        _pathQueue = new Queue<Vector3Int>(path);
        _pathTargetCell = targetCell;
        _pathMoveToken++;
        MoveNextPathStep(_pathMoveToken);
    }

    /// <summary>
    /// 执行路径中的下一步移动。
    /// </summary>
    /// <param name="token">路径移动令牌，用于打断流程。</param>
    private void MoveNextPathStep(int token)
    {
        if (token != _pathMoveToken || !_isPathMoving || _pathQueue == null || _pathQueue.Count == 0)
        {
            FinishPathMove();
            return;
        }

        var nextCell = _pathQueue.Dequeue();
        bool isFinalStep = _pathTargetCell.HasValue && nextCell == _pathTargetCell.Value;
        if (!isFinalStep && !IsCellWalkable(nextCell))
        {
            FinishPathMove();
            return;
        }

        Vector3Int currentCell = _gridManager.MapGrid.WorldToCell(transform.position);
        Vector2 dir = new(nextCell.x - currentCell.x, nextCell.y - currentCell.y);
        UpdateMoveDirection(dir);

        Vector2 targetPos = GetWorldPosForCell(nextCell);
        TryStartPathStep(nextCell, targetPos, isFinalStep, token);
    }

    /// <summary>
    /// 结束路径移动状态并清理缓存。
    /// </summary>
    private void FinishPathMove()
    {
        _isPathMoving = false;
        _pathQueue = null;
        _pathTargetCell = null;
    }

    /// <summary>
    /// 立即打断路径移动并停止当前移动协程。
    /// </summary>
    private void CancelPathMove()
    {
        _pathMoveToken++;
        _moveToken++;
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        _isMoving = false;
        _waitingForEventExecution = false;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        FinishPathMove();
        OnMoveStateChanged?.Invoke(this, new PlayerMoveStateChangedEventArgs { IsMoving = false, MoveTime = 0 });
    }

    /// <summary>
    /// 校验并启动单步移动，必要时触发事件判断。
    /// </summary>
    /// <param name="targetCell">本步目标格子。</param>
    /// <param name="targetPos">本步目标世界坐标。</param>
    /// <param name="allowEvent">是否允许触发事件格子。</param>
    /// <param name="token">路径移动令牌。</param>
    private void TryStartPathStep(Vector3Int targetCell, Vector2 targetPos, bool allowEvent, int token)
    {
        if (token != _pathMoveToken || !_isPathMoving) return;
        if (_gridManager == null || !_gridManager.IsInGridBounds(targetCell))
        {
            FinishPathMove();
            return;
        }

        if (_gridManager.GetGroundTileAtCell(targetCell) == null)
        {
            NotifyBlockedMovement(targetPos);
            FinishPathMove();
            return;
        }

        if (_gridManager.GetObstacleTileAtCell(targetCell) != null)
        {
            NotifyBlockedMovement(targetPos);
            FinishPathMove();
            return;
        }

        if (!allowEvent && _gridManager.GetEventTileAtCell(targetCell) != null)
        {
            NotifyBlockedMovement(targetPos);
            FinishPathMove();
            return;
        }

        _eventNodeManager.RequestEnterCell_PreMove(targetCell, _globalEventVariables.GetInt(GlobalEventKey.LayerId),
            (allowEnter, blockUntilComplete) =>
            {
                if (token != _pathMoveToken || !_isPathMoving) return;
                if (!allowEnter)
                {
                    NotifyBlockedMovement(targetPos);
                    FinishPathMove();
                    return;
                }

                StartMoveProcess(targetPos, targetCell, blockUntilComplete, () => MoveNextPathStep(token));
            },
            null
        );
    }

    /// <summary>
    /// 更新玩家朝向并驱动移动动画参数。
    /// </summary>
    /// <param name="dir">当前移动方向。</param>
    private void UpdateMoveDirection(Vector2 dir)
    {
        if (dir == Vector2.zero) return;
        OnMoveDirectionChanged?.Invoke(this, new PlayerMoveDirectionChangedEventArgs
        {
            Horizontal = dir.x,
            Vertical = dir.y
        });
    }

    /// <summary>
    /// 使用 BFS 构建从起点到终点的路径。
    /// </summary>
    /// <param name="startCell">起点格子。</param>
    /// <param name="targetCell">终点格子。</param>
    /// <returns>路径列表（不含起点），若不可达则返回 null。</returns>
    private List<Vector3Int> BuildPath(Vector3Int startCell, Vector3Int targetCell)
    {
        var visited = new HashSet<Vector3Int> { startCell };
        var queue = new Queue<Vector3Int>();
        var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
        queue.Enqueue(startCell);

        Vector3Int[] directions =
        {
            Vector3Int.up,
            Vector3Int.down,
            Vector3Int.left,
            Vector3Int.right
        };

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == targetCell)
            {
                return ReconstructPath(cameFrom, startCell, targetCell);
            }

            foreach (var dir in directions)
            {
                var next = current + dir;
                if (visited.Contains(next)) continue;
                if (!IsCellWalkable(next))
                {
                    if (next != targetCell || !IsTargetReachable(next)) continue;
                }
                visited.Add(next);
                cameFrom[next] = current;
                queue.Enqueue(next);
            }
        }

        return null;
    }

    /// <summary>
    /// 回溯路径并生成按顺序排列的格子列表。
    /// </summary>
    /// <param name="cameFrom">路径回溯字典。</param>
    /// <param name="startCell">起点格子。</param>
    /// <param name="targetCell">终点格子。</param>
    /// <returns>路径列表（不含起点）。</returns>
    private List<Vector3Int> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int startCell, Vector3Int targetCell)
    {
        var path = new List<Vector3Int>();
        var current = targetCell;
        while (current != startCell)
        {
            path.Add(current);
            if (!cameFrom.TryGetValue(current, out current))
            {
                return null;
            }
        }
        path.Reverse();
        return path;
    }

    /// <summary>
    /// 判断格子是否可通行（空地且非障碍/事件）。
    /// </summary>
    /// <param name="cellPos">格子坐标。</param>
    /// <returns>是否可通行。</returns>
    private bool IsCellWalkable(Vector3Int cellPos)
    {
        if (_gridManager == null) return false;
        if (!_gridManager.IsInGridBounds(cellPos)) return false;
        if (_gridManager.GetGroundTileAtCell(cellPos) == null) return false;
        if (_gridManager.GetObstacleTileAtCell(cellPos) != null) return false;
        if (_gridManager.GetEventTileAtCell(cellPos) != null) return false;
        return true;
    }

    /// <summary>
    /// 判断目标格子是否具备“尝试进入”的基础条件。
    /// </summary>
    /// <param name="cellPos">目标格子坐标。</param>
    /// <returns>是否允许尝试进入。</returns>
    private bool IsTargetReachable(Vector3Int cellPos)
    {
        if (_gridManager == null) return false;
        if (!_gridManager.IsInGridBounds(cellPos)) return false;
        return _gridManager.GetGroundTileAtCell(cellPos) != null;
    }

    /// <summary>
    /// 将格子坐标转换为玩家对齐用的世界坐标。
    /// </summary>
    /// <param name="cellPos">格子坐标。</param>
    /// <returns>玩家对齐的世界坐标。</returns>
    private Vector2 GetWorldPosForCell(Vector3Int cellPos)
    {
        Vector2 center = _gridManager.GetCellCenterWorld(cellPos);
        float halfSize = _gridManager.tileSize * 0.5f;
        return center - new Vector2(halfSize, halfSize);
    }

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