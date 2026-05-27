using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Modules.Core.DataDefine;
using Modules.Core.Runtime;
using Modules.EventSystem.DataDefine.EventArgs;
using Modules.Map.Runtime;
using Modules.Map.Runtime.EventTile;
using TriInspector;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace Modules.Player.Runtime.Movement
{
    public class PlayerMovement : MonoBehaviour
    {
        private const int MinMoveSpeed = 1;
        private const int MaxMoveSpeed = 20;

        // Inspector配置项
        [Title("资源配置")] [Slider(nameof(MinMoveSpeed), nameof(MaxMoveSpeed))] [LabelText("移动速度")]
        public float moveSpeed = 5f;

        [FormerlySerializedAs("rb")] [LabelText("组件引用")] [SerializeField]
        private Rigidbody2D playerRigidbody2D;

        [LabelText("组件引用")] [SerializeField] private LineRenderer pathLineRenderer;

        [FormerlySerializedAs("_pathLineMaterial")] [LabelText("线材质")] [SerializeField]
        private Material pathLineMaterial;

        // 组件引用
        private EventCenter _eventCenter;
        private EventTileManager _eventNodeManager;
        private IGlobalEventVariables _globalEventVariables;
        private GridManager _gridManager;

        // 事件
        public event EventHandler<PlayerMoveDirectionChangedEventArgs> OnMoveDirectionChanged;
        public event EventHandler<PlayerMoveStateChangedEventArgs> OnMoveStateChanged;

        /// <summary>
        ///     移动指令类型，用于区分单步移动与路径移动。
        /// </summary>
        private enum PendingMoveCommandType
        {
            None,
            StepDirection,
            PathToCell
        }

        #region 运行时状态

        // 事件订阅状态
        private bool _eventSubscribed;

        // 移动状态
        private bool _isMoving;
        private bool _waitingForEventExecution;
        private Vector2 _moveDir;
        private Coroutine _moveCoroutine;

        // 路径移动状态
        private bool _isPathMoving;
        private int _moveToken;
        private int _pathMoveToken;
        private Queue<Vector3Int> _pathQueue;
        private Vector3Int? _pathTargetCell;
        private Vector2 _targetWorldPos;
        private Vector3Int? _currentStepTargetCell;

        // 移动指令缓冲（仅保留最新）
        private PendingMoveCommandType _pendingMoveCommandType;
        private Vector2 _pendingMoveDirection;
        private Vector3Int _pendingTargetCell;

        #endregion

        #region 生命周期

        private void OnEnable()
        {
            SubscribeEventCenter();
        }

        /// <summary>
        ///     注入并保存所需依赖项，然后订阅事件中心。
        /// </summary>
        /// <remarks>通过依赖注入调用，应仅执行一次以避免重复订阅。</remarks>
        /// <param name="globalEventVariables">用于访问和管理全局事件变量。</param>
        /// <param name="eventCenter">用于事件的分发与订阅。</param>
        /// <param name="gridManager">用于管理网格数据与坐标信息。</param>
        /// <param name="eventNodeManager">用于管理场景中的事件节点或格子事件。</param>
        [Inject]
        public void Construct(IGlobalEventVariables globalEventVariables, EventCenter eventCenter,
            GridManager gridManager, EventTileManager eventNodeManager)
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

        private void SubscribeEventCenter()
        {
            if (!_eventCenter || _eventSubscribed) return;
            _eventCenter.OnLayerSwitched += OnLayerSwitched;
            _eventSubscribed = true;
        }

        private void UnsubscribeEventCenter()
        {
            if (!_eventCenter || !_eventSubscribed) return;
            _eventCenter.OnLayerSwitched -= OnLayerSwitched;
            _eventSubscribed = false;
        }

        #endregion

        #region 输入处理

        /// <summary>
        ///     响应楼层切换事件，将玩家传送到事件指定的出生点并记录移动日志。
        /// </summary>
        /// <remarks>调用 TeleportToPosition 并传入第二个参数 false（表示不触发过渡效果），随后记录玩家已移动到新楼层出生点的日志。</remarks>
        /// <param name="sender">触发事件的对象。</param>
        /// <param name="args">包含楼层切换信息的事件参数，例如 SpawnPos（新的出生位置）。</param>
        private void OnLayerSwitched(object sender, LayerSwitchedEventArgs args)
        {
            TeleportToPosition(args.SpawnPos, false);
            DebugEditor.Log($"玩家已移动到新楼层出生点：{args.SpawnPos}");
        }

        /// <summary>
        ///     处理玩家的移动输入：判断是否应忽略输入；在角色正在移动时缓存一步移动命令，否则执行一步移动并在完成后处理挂起的移动。
        /// </summary>
        /// <remarks>
        ///     若 ShouldIgnoreMoveInput 返回 true 则直接返回；若当前正在移动则调用 TryCacheStepMoveCommand 缓存命令，
        ///     否则调用 ExecuteStepMove 并在移动完成回调中处理挂起移动。
        /// </remarks>
        /// <param name="moveDirection">玩家输入的移动方向。</param>
        public void HandleStepMoveInput(Vector2 moveDirection)
        {
            // 统一判断是否应忽略移动输入
            if (IsMovementInputBlocking() || moveDirection == Vector2.zero)
            {
                ClearPendingMoveCommand();
                return;
            }

            if (_isMoving)
            {
                TryCacheStepMoveCommand(moveDirection);
                return;
            }

            ExecuteStepMove(moveDirection, () => HandlePendingMoveAfterStep(_pathMoveToken, null));
        }

        /// <summary>
        ///     路径移动入口
        /// </summary>
        /// <param name="worldPos">目标位置的世界坐标。</param>
        /// <returns>当移动已开始或移动命令已缓存时返回 true；当命令被取消或未执行时返回 false。</returns>
        public void HandlePathMoveInput(Vector2 worldPos)
        {
            if (IsMovementInputBlocking())
            {
                ClearPendingMoveCommand();
                return;
            }

            _gridManager.TryWorldToCellPos(worldPos, out var targetCell);
            if (_isMoving)
            {
                TryCachePathMoveCommand(targetCell);
                return;
            }

            ExecutePathMove(targetCell);
        }

        #endregion

        #region 缓冲指令

        /// <summary>
        ///     尝试在移动协程进行中写入缓冲的单步移动指令。
        /// </summary>
        /// <param name="direction">移动方向。</param>
        private void TryCacheStepMoveCommand(Vector2 direction)
        {
            var previewTargetCell = ComputeTargetCellPosAndUpdateWorldPos(direction);
            TryCacheMoveCommand(previewTargetCell, () =>
            {
                _pendingMoveCommandType = PendingMoveCommandType.StepDirection;
                _pendingMoveDirection = direction;
            });
        }

        private void TryCachePathMoveCommand(Vector3Int targetCell)
        {
            TryCacheMoveCommand(targetCell, () =>
            {
                _pendingMoveCommandType = PendingMoveCommandType.PathToCell;
                _pendingTargetCell = targetCell;
            });
        }

        /// <summary>
        ///     尝试写入通用的缓冲移动指令，统一处理与当前步目标重复的情况。
        /// </summary>
        /// <param name="targetCell">待缓存的目标格子。</param>
        /// <param name="cacheAction">具体的缓存写入逻辑。</param>
        private void TryCacheMoveCommand(Vector3Int targetCell, Action cacheAction)
        {
            // 统一处理与当前步目标重复的情况
            if (IsSameAsCurrentStepTarget(targetCell))
            {
                ClearPendingMoveCommand();
                return;
            }

            cacheAction?.Invoke();
        }

        /// <summary>
        ///     在当前一步移动完成后处理缓冲的移动指令。
        /// </summary>
        /// <param name="token">路径移动令牌。</param>
        /// <param name="continuePath">继续当前路径移动的回调。</param>
        private void HandlePendingMoveAfterStep(int token, Action continuePath)
        {
            if (_pendingMoveCommandType == PendingMoveCommandType.None)
            {
                continuePath?.Invoke();
                return;
            }

            if (IsMovementInputBlocking())
            {
                ClearPendingMoveCommand();
                continuePath?.Invoke();
                return;
            }

            switch (_pendingMoveCommandType)
            {
                case PendingMoveCommandType.StepDirection:
                    var pendingDirection = _pendingMoveDirection;
                    if (_isPathMoving) FinishPathMove();
                    ClearPendingMoveCommand();
                    ExecuteStepMove(pendingDirection, () => HandlePendingMoveAfterStep(_pathMoveToken, null));
                    break;
                case PendingMoveCommandType.PathToCell:
                    var pendingTargetCell = _pendingTargetCell;
                    if (_isPathMoving) FinishPathMove();
                    ClearPendingMoveCommand();
                    ExecutePathMove(pendingTargetCell);
                    break;
                case PendingMoveCommandType.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion

        #region 玩家移动

        /// <summary>
        ///     执行单步移动（统一入口）。
        /// </summary>
        /// <param name="direction">移动方向。</param>
        /// <param name="onStepComplete">单步移动完成后的回调。</param>
        private void ExecuteStepMove(Vector2 direction, Action onStepComplete)
        {
            UpdateMoveDirection(direction);
            var targetCell = ComputeTargetCellPosAndUpdateWorldPos(direction);
            if (!IsCellBasicPassable(targetCell))
            {
                NotifyBlockedMovement(_targetWorldPos);
                return;
            }

            _eventNodeManager.RequestEnterCell_PreMove(targetCell, _globalEventVariables.GetInt(GlobalEventKey.LayerId),
                (allowEnter, blockUntilComplete) =>
                {
                    if (!allowEnter)
                    {
                        NotifyBlockedMovement(_targetWorldPos);
                        return;
                    }

                    StartMoveProcess(_targetWorldPos, blockUntilComplete, () =>
                    {
                        _waitingForEventExecution = false;
                        onStepComplete?.Invoke();
                    });
                }
            );
        }

        /// <summary>
        ///     执行路径移动（统一入口）。
        /// </summary>
        /// <param name="targetCell">目标格子坐标。</param>
        /// <returns>是否成功启动路径移动。</returns>
        private void ExecutePathMove(Vector3Int targetCell)
        {
            if (!TryGetCurrentCell(out var startCell))
                return;
            if (startCell == targetCell) return;

            var path = BuildPath(startCell, targetCell);
            if (path == null || path.Count == 0) return;

            StartPathMoveProcess(path, targetCell);
        }

        #region 路径移动函数

        /// <summary>
        ///     启动路径移动流程，并缓存路径队列。
        /// </summary>
        /// <param name="path">按顺序的路径格子列表（不含起点）。</param>
        /// <param name="targetCell">点击的目标格子。</param>
        private void StartPathMoveProcess(List<Vector3Int> path, Vector3Int targetCell)
        {
            _isPathMoving = true;
            _pathQueue = new Queue<Vector3Int>(path);
            _pathTargetCell = targetCell;
            _pathMoveToken++;
            MoveNextPathStep(_pathMoveToken);
        }

        /// <summary>
        ///     执行路径中的下一步移动。
        /// </summary>
        /// <param name="token">路径移动令牌，用于打断流程。</param>
        private void MoveNextPathStep(int token)
        {
            if (token != _pathMoveToken || !_isPathMoving || _pathQueue == null || _pathQueue.Count == 0)
            {
                FinishPathMove();
                return;
            }

            RenderPathLine();
            var nextCell = _pathQueue.Dequeue();
            var isFinalStep = _pathTargetCell.HasValue && nextCell == _pathTargetCell.Value;
            if ((!isFinalStep && !IsCellBasicPassable(nextCell)) || !TryGetCurrentCell(out var currentCell))
            {
                FinishPathMove();
                return;
            }

            Vector2 dir = new(nextCell.x - currentCell.x, nextCell.y - currentCell.y);
            UpdateMoveDirection(dir);

            var targetWorldPos = _gridManager.GetCellOriginWorld(nextCell);
            TryStartPathStep(nextCell, targetWorldPos, isFinalStep, token);
        }

        /// <summary>
        ///     校验并启动单步移动，必要时触发事件判断。
        /// </summary>
        /// <param name="targetCell">本步目标格子。</param>
        /// <param name="targetPos">本步目标世界坐标。</param>
        /// <param name="allowEvent">是否允许触发事件格子。</param>
        /// <param name="token">路径移动令牌。</param>
        private void TryStartPathStep(Vector3Int targetCell, Vector2 targetPos, bool allowEvent, int token)
        {
            if (token != _pathMoveToken || !_isPathMoving) return;

            if (!allowEvent && _gridManager.GetEventTileAtCell(targetCell))
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

                    StartMoveProcess(targetPos, blockUntilComplete,
                        () => HandlePendingMoveAfterStep(token, () => MoveNextPathStep(token)));
                }
            );
        }

        /// <summary>
        ///     结束路径移动状态并清理缓存。
        /// </summary>
        private void FinishPathMove()
        {
            _isPathMoving = false;
            _pathQueue = null;
            _pathTargetCell = null;
            ClearPathLine();
        }


        /// <summary>
        ///     绘制当前路径线（起点-中间点-终点）。
        /// </summary>
        private void RenderPathLine()
        {
            if (!pathLineRenderer || !_isPathMoving || _pathTargetCell == null) return;

            if (!TryGetCurrentCell(out var currentCell))
            {
                pathLineRenderer.positionCount = 0;
                return;
            }

            var currentPos = _gridManager.GetCellCenterWorld(currentCell);
            var points = new List<Vector3> { currentPos };

            if (_pathQueue != null)
                points.AddRange(_pathQueue
                    .Select(cell => _gridManager.GetCellCenterWorld(cell))
                    .Select(dummy => (Vector3)dummy));

            if (points.Count < 2)
            {
                pathLineRenderer.positionCount = 0;
                return;
            }

            pathLineRenderer.useWorldSpace = true;
            pathLineRenderer.positionCount = points.Count;
            pathLineRenderer.SetPositions(points.ToArray());
            pathLineRenderer.enabled = true;
        }

        /// <summary>
        ///     清除路径线。
        /// </summary>
        private void ClearPathLine()
        {
            if (!pathLineRenderer) return;

            pathLineRenderer.positionCount = 0;
            pathLineRenderer.enabled = false;
        }

        /// <summary>
        ///     使用 BFS 构建从起点到终点的路径。
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
                if (current == targetCell) return ReconstructPath(cameFrom, startCell, targetCell);

                foreach (var dir in directions)
                {
                    var next = current + dir;
                    if (visited.Contains(next)) continue;
                    if (!IsCellDirectlyPassable(next))
                        if (next != targetCell || !IsCellBasicPassable(next))
                            continue;
                    visited.Add(next);
                    cameFrom[next] = current;
                    queue.Enqueue(next);
                }
            }

            return null;
        }

        /// <summary>
        ///     回溯路径并生成按顺序排列的格子列表。
        /// </summary>
        /// <param name="cameFrom">路径回溯字典。</param>
        /// <param name="startCell">起点格子。</param>
        /// <param name="targetCell">终点格子。</param>
        /// <returns>路径列表（不含起点）。</returns>
        private List<Vector3Int> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int startCell,
            Vector3Int targetCell)
        {
            var path = new List<Vector3Int>();
            var current = targetCell;
            while (current != startCell)
            {
                path.Add(current);
                if (!cameFrom.TryGetValue(current, out current)) return null;
            }

            path.Reverse();
            return path;
        }

        #endregion

        #region 核心移动函数

        /// <summary>
        ///     开始玩家移动流程：停止当前移动协程（如有），启动移动到指定世界位置的协程，发布移动状态事件并在到达时触发到达事件；根据 blockUntilComplete 决定是否在事件执行完成前阻塞玩家并在适当时机调用回调。
        /// </summary>
        /// <remarks>
        ///     方法会递增内部移动令牌并计算移动时间，通过 OnMoveStateChanged 通知移动开始；到达目标后通过 _eventCenter 触发
        ///     PlayerArrivedEventArgs。若已有移动协程存在会先停止该协程。
        /// </remarks>
        /// <param name="targetPos">目标世界位置（Vector2），玩家将移动到该位置。</param>
        /// <param name="blockUntilComplete">指示在触发到达事件后是否阻塞玩家，若为 true 则等待事件执行完成并由外部解除阻塞。</param>
        /// <param name="onExecutionComplete">可选回调；当移动完成且未阻塞时立即调用，或在阻塞情况下由外部在事件执行完成时调用。</param>
        private void StartMoveProcess(Vector2 targetPos, bool blockUntilComplete, Action onExecutionComplete)
        {
            var token = ++_moveToken;
            var moveTime = _gridManager.tileSize / moveSpeed;
            OnMoveStateChanged?.Invoke(this,
                new PlayerMoveStateChangedEventArgs { IsMoving = true, MoveTime = moveTime });

            if (_gridManager.TryWorldToCellPos(targetPos, out var targetCell))
                _currentStepTargetCell = targetCell;

            StopMoveCoroutine();

            // 如果事件要求阻塞玩家直到执行完成，则设置标志（等待 EventTileManager 在完成时触发解锁回调）
            if (blockUntilComplete) _waitingForEventExecution = true;

            _moveCoroutine = StartCoroutine(MoveToTarget(targetPos, token, () =>
            {
                // 到达后触发玩家移动事件（由订阅方决定是否响应）
                _eventCenter.TriggerPlayerArrived(new PlayerArrivedEventArgs
                    { TriggerEvent = true, TargetWorldPos = targetPos });
                // 如果事件不要求阻塞，则立即解锁（若要求阻塞则会由 EventTileManager 在事件完成时调用 onExecutionComplete）
                if (!blockUntilComplete)
                {
                    _waitingForEventExecution = false;
                    onExecutionComplete?.Invoke();
                }

                // 如果 blockUntilComplete 且 onExecutionComplete 非 null，则保持等待（由外部回调解除）
                _moveCoroutine = null;
            }));
        }

        /// <summary>
        ///     在物理帧中平滑移动对象到指定目标，直到到达或移动令牌失效，并在完成后触发可选回调。
        /// </summary>
        /// <remarks>
        ///     使用 Rigidbody.MovePosition 在 FixedUpdate 周期内移动以保持与物理和动画同步；到达目标后会将 transform 和
        ///     Rigidbody 的位置对齐，并重置移动状态与相应事件通知。
        /// </remarks>
        /// <param name="targetPos">目标位置（世界坐标）。</param>
        /// <param name="token">用于判断当前移动是否仍有效的令牌；令牌不匹配时中止移动。</param>
        /// <param name="onComplete">可选回调，移动完成且未被取消时调用。</param>
        /// <returns>一个用于在 FixedUpdate 驱动下执行移动的 IEnumerator 协程。</returns>
        private IEnumerator MoveToTarget(Vector2 targetPos, int token, Action onComplete = null)
        {
            _isMoving = true;
            // 物理帧驱动 → 移动更稳定，和动画同步
            while (token == _moveToken && Vector2.SqrMagnitude((Vector2)transform.position - targetPos) > 0.000001f)
            {
                playerRigidbody2D.MovePosition(Vector2.MoveTowards(transform.position, targetPos,
                    moveSpeed * Time.fixedDeltaTime));
                yield return new WaitForFixedUpdate();
            }

            if (token != _moveToken) yield break;

            // 精准到位 + 重置动画状态
            transform.position = targetPos;
            playerRigidbody2D.position = targetPos;
            _isMoving = false;
            OnMoveStateChanged?.Invoke(this, new PlayerMoveStateChangedEventArgs
            {
                IsMoving = false,
                MoveTime = 0
            });

            _currentStepTargetCell = null;

            onComplete?.Invoke();
        }

        /// <summary>
        ///     将玩家立即传送到指定的世界坐标，同时停止当前移动、清除路径并更新移动状态，且可选择触发到达事件。
        /// </summary>
        /// <remarks>
        ///     会停止并清理现有的移动协程和路径线，设置 _isMoving 为 false 并通过 OnMoveStateChanged 通知状态变更；直接设置
        ///     transform.position 与 playerRigidbody2D.position 以避免物理问题；最后调用事件中心触发到达事件（若 triggerEvent 为 true）。
        /// </remarks>
        /// <param name="targetPos">要传送到的目标世界坐标。</param>
        /// <param name="triggerEvent">是否触发玩家到达事件；默认为 true。</param>
        public void TeleportToPosition(Vector2 targetPos, bool triggerEvent = true)
        {
            StopMoveCoroutine();
            ClearPathLine();
            ClearPendingMoveCommand();

            _isMoving = false;
            _waitingForEventExecution = false;
            OnMoveStateChanged?.Invoke(this, new PlayerMoveStateChangedEventArgs
            {
                IsMoving = false,
                MoveTime = 0
            });

            // 直接设置位置（同时更新刚体位置避免物理问题）
            transform.position = targetPos;
            playerRigidbody2D.position = targetPos;

            _targetWorldPos = targetPos;

            _eventCenter.TriggerPlayerArrived(new PlayerArrivedEventArgs
            {
                TriggerEvent = triggerEvent,
                TargetWorldPos = targetPos
            });
            DebugEditor.Log($"玩家已传送至: {targetPos}");
        }

        #endregion

        #endregion

        #region 辅助函数

        #region 计算与转换函数

        /// <summary>
        ///     基础通行检查（不含事件处理）：判断指定格子是否可通行（边界、Ground 是否存在、Obstacle 是否为空）。
        /// </summary>
        /// <param name="cellPos">目标格子坐标。</param>
        /// <returns>是否可通行（满足基础条件返回 true，否则 false）。</returns>
        private bool IsCellBasicPassable(Vector3Int cellPos)
        {
            if (!_gridManager) return false;
            if (!_gridManager.IsInGridBounds(cellPos)) return false;
            if (!_gridManager.GetGroundTileAtCell(cellPos)) return false;
            return !_gridManager.GetObstacleTileAtCell(cellPos);
        }

        /// <summary>
        ///     判断格子是否可直接通行（基础检查+无事件）。
        /// </summary>
        /// <param name="cellPos">格子坐标。</param>
        /// <returns>是否可直接通行。</returns>
        private bool IsCellDirectlyPassable(Vector3Int cellPos)
        {
            if (!_gridManager) return false;
            return IsCellBasicPassable(cellPos) && !_gridManager.GetEventTileAtCell(cellPos);
        }

        /// <summary>
        ///     计算基于给定方向的目标格子坐标，并更新对应的世界位置。
        /// </summary>
        /// <remarks>_targetWorldPos 在成功计算目标格子后被设置为该格子的世界原点；方法还会输出调试日志以记录当前格子与目标格子信息。</remarks>
        /// <param name="dir">表示相对格子偏移的二维方向向量，分量将使用四舍五入转换为整数格偏移。</param>
        /// <returns>返回计算得到的目标格子坐标（Vector3Int）；若无法将当前位置转换为格子坐标则返回 Vector3Int.zero。</returns>
        private Vector3Int ComputeTargetCellPosAndUpdateWorldPos(Vector2 dir)
        {
            if (!TryGetCurrentCell(out var currentCell)) return Vector3Int.zero;
            var offset = new Vector3Int(Mathf.RoundToInt(dir.x), Mathf.RoundToInt(dir.y), 0);
            var targetCell = currentCell + offset;
            _targetWorldPos = _gridManager.GetCellOriginWorld(targetCell);
            return targetCell;
        }

        /// <summary>
        ///     确定当前是否应阻止移动输入。
        /// </summary>
        /// <remarks>
        ///     当正在等待事件执行（_waitingForEventExecution 为 true）或全局事件变量 DialogueIsActive 为 true
        ///     时，将阻止移动输入并返回 true。
        /// </remarks>
        /// <returns>若应阻止移动输入则返回 true；否则返回 false。</returns>
        private bool IsMovementInputBlocking()
        {
            return _waitingForEventExecution || _globalEventVariables.GetBool(GlobalEventKey.DialogueIsActive);
        }

        /// <summary>
        ///     尝试获取玩家当前所在的格子坐标。
        /// </summary>
        /// <param name="currentCell">输出的当前格子坐标。</param>
        /// <returns>转换成功返回 true，否则返回 false。</returns>
        private bool TryGetCurrentCell(out Vector3Int currentCell)
        {
            currentCell = Vector3Int.zero;
            return _gridManager &&
                   // 统一通过 GridManager 进行世界坐标到格子坐标的转换
                   _gridManager.TryWorldToCellPos(transform.position, out currentCell);
        }

        /// <summary>
        ///     停止当前移动协程并清理引用。
        /// </summary>
        private void StopMoveCoroutine()
        {
            if (_moveCoroutine == null) return;
            StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
        }

        /// <summary>
        ///     在移动被阻止时，将玩家移动状态设置为已停止并触发一个不触发额外处理的到达事件。
        /// </summary>
        /// <remarks>
        ///     该方法通过 OnMoveStateChanged 将 IsMoving 设为 false 并将 MoveTime 重置为 0，随后通过 _eventCenter 触发
        ///     PlayerArrivedEventArgs（TriggerEvent 为 false）。
        /// </remarks>
        /// <param name="blockedTargetWorldPos">表示尝试移动但被阻止的目标位置（世界坐标）。</param>
        private void NotifyBlockedMovement(Vector2 blockedTargetWorldPos)
        {
            OnMoveStateChanged?.Invoke(this, new PlayerMoveStateChangedEventArgs { IsMoving = false, MoveTime = 0 });
            _eventCenter.TriggerPlayerArrived(new PlayerArrivedEventArgs
                { TriggerEvent = false, TargetWorldPos = blockedTargetWorldPos });
        }

        /// <summary>
        ///     更新玩家朝向并驱动移动动画参数。
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
        ///     清除待执行的移动指令缓冲。
        /// </summary>
        private void ClearPendingMoveCommand()
        {
            _pendingMoveCommandType = PendingMoveCommandType.None;
            _pendingMoveDirection = Vector2.zero;
            _pendingTargetCell = Vector3Int.zero;
        }

        /// <summary>
        ///     判断目标格是否与当前一步移动目标一致。
        /// </summary>
        /// <param name="targetCell">目标格子坐标。</param>
        /// <returns>一致返回 true，否则返回 false。</returns>
        private bool IsSameAsCurrentStepTarget(Vector3Int targetCell)
        {
            return _currentStepTargetCell.HasValue && _currentStepTargetCell.Value == targetCell;
        }

        #endregion

        #endregion
    }
}