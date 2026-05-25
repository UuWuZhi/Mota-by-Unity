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
        public event EventHandler<PlayerInputEventArgs> OnMoveInput;
        public event EventHandler<PlayerMoveDirectionChangedEventArgs> OnMoveDirectionChanged;
        public event EventHandler<PlayerMoveStateChangedEventArgs> OnMoveStateChanged;

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

        #endregion

        #region 事件系统

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
        ///     处理玩家移动输入：验证状态与输入有效性，触发方向更新，并根据格子通行性与事件管理器决定并启动移动流程。
        /// </summary>
        /// <remarks>
        ///     调用时会先触发 OnMoveInput 事件并在路径移动时取消路径移动。若处于等待事件执行或对话激活，则忽略输入；若已在移动、输入无效或方向为零则直接返回。方法会通过
        ///     OnMoveDirectionChanged
        ///     同步动画混合树参数，计算目标格子并进行基础通行性检查；若不可通行则通知被阻挡。是否允许进入目标格子的最终决定交由事件管理器（RequestEnterCell_PreMove），在允许进入时调用
        ///     StartMoveProcess，并可根据事件执行策略在事件完成前阻塞玩家输入；事件完成后通过回调解除阻塞。
        /// </remarks>
        /// <param name="args">包含玩家输入的移动方向、输入有效性等信息，用于计算目标格子并驱动移动逻辑。</param>
        public void HandleMoveInput(PlayerInputEventArgs args)
        {
            OnMoveInput?.Invoke(this, args);
            // 统一判断是否应忽略移动输入
            if (ShouldIgnoreMoveInput(args)) return;
            if (_isPathMoving) CancelPathMove();

            // 1. 拿到事件传递的移动方向
            var dir = args.MoveDirection;
            var horizontal = dir.x;
            var vertical = dir.y;
            // 2. 同步Blender混合树的horizontal/vertical参数
            OnMoveDirectionChanged?.Invoke(this, new PlayerMoveDirectionChangedEventArgs
            {
                Horizontal = horizontal,
                Vertical = vertical
            });
            // 3. 计算目标位置
            var targetCell = ComputeTargetCellPosAndUpdateWorldPos(dir);
            // 4. 基础通行检查（不含事件处理）
            if (!IsCellBasicPassable(targetCell))
            {
                NotifyBlockedMovement(_targetWorldPos);
                return;
            }

            // 5. 委托给 EventTileManager 决定是否允许进入以及是否在事件执行期间阻塞玩家
            _eventNodeManager.RequestEnterCell_PreMove(targetCell, _globalEventVariables.GetInt(GlobalEventKey.LayerId),
                (allowEnter, blockUntilComplete) =>
                {
                    if (!allowEnter)
                    {
                        // 阻止移动
                        NotifyBlockedMovement(_targetWorldPos);
                        return;
                    }

                    // 允许进入
                    StartMoveProcess(_targetWorldPos, blockUntilComplete, () =>
                    {
                        // 当事件执行决定阻塞玩家直到完成时，EventTileManager 会在后台执行并在完成时调用此回调
                        // 该回调用于解锁玩家输入。
                        _waitingForEventExecution = false;
                    });
                }
            );
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

        #endregion

        #region 玩家移动函数

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

        #region 辅助函数

        /// <summary>
        ///     尝试通过鼠标点击发起逐步移动（世界坐标入口）。
        /// </summary>
        /// <param name="worldPos">鼠标点击的世界坐标。</param>
        /// <returns>是否成功发起逐步移动。</returns>
        public bool TryMoveToWorldPosStep(Vector2 worldPos)
        {
            if (!_gridManager || _globalEventVariables == null) return false;
            return _gridManager.TryWorldToCellPos(worldPos, out var targetCell) && TryMoveToCellStep(targetCell);
        }

        /// <summary>
        ///     尝试通过鼠标点击发起逐步移动（格子坐标入口）。
        /// </summary>
        /// <param name="targetCell">目标格子坐标。</param>
        /// <returns>是否成功发起逐步移动。</returns>
        private bool TryMoveToCellStep(Vector3Int targetCell)
        {
            if (_isPathMoving || _isMoving) CancelPathMove();
            if (_waitingForEventExecution) return false;
            if (_globalEventVariables.GetBool(GlobalEventKey.DialogueIsActive)) return false;
            if (!_gridManager || !_gridManager.IsInGridBounds(targetCell)) return false;

            if (!TryGetCurrentCell(out var startCell))
                return false;
            if (startCell == targetCell) return false;
            var path = BuildPath(startCell, targetCell);
            if (path == null || path.Count == 0) return false;

            StartPathMove(path, targetCell);
            return true;
        }

        /// <summary>
        ///     启动路径移动流程，并缓存路径队列。
        /// </summary>
        /// <param name="path">按顺序的路径格子列表（不含起点）。</param>
        /// <param name="targetCell">点击的目标格子。</param>
        private void StartPathMove(List<Vector3Int> path, Vector3Int targetCell)
        {
            _isPathMoving = true;
            _pathQueue = new Queue<Vector3Int>(path);
            _pathTargetCell = targetCell;
            _pathMoveToken++;
            //RenderPathLine();
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
            if (!isFinalStep && !IsCellBasicPassable(nextCell))
            {
                FinishPathMove();
                return;
            }

            if (!TryGetCurrentCell(out var currentCell))
            {
                FinishPathMove();
                return;
            }

            Vector2 dir = new(nextCell.x - currentCell.x, nextCell.y - currentCell.y);
            UpdateMoveDirection(dir);
            //RenderPathLine();

            var targetPos = _gridManager.GetCellOriginWorld(nextCell);
            TryStartPathStep(nextCell, targetPos, isFinalStep, token);
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
        ///     立即打断路径移动并停止当前移动协程。
        /// </summary>
        private void CancelPathMove()
        {
            _pathMoveToken++;
            _moveToken++;
            StopMoveCoroutine();

            _isMoving = false;
            _waitingForEventExecution = false;
            if (playerRigidbody2D)
            {
                playerRigidbody2D.velocity = Vector2.zero;
                playerRigidbody2D.angularVelocity = 0f;
            }

            FinishPathMove();
            OnMoveStateChanged?.Invoke(this, new PlayerMoveStateChangedEventArgs { IsMoving = false, MoveTime = 0 });
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
            if (!_gridManager || !_gridManager.IsInGridBounds(targetCell))
            {
                FinishPathMove();
                return;
            }

            if (!_gridManager.GetGroundTileAtCell(targetCell) ||
                _gridManager.GetObstacleTileAtCell(targetCell) ||
                (!allowEvent && _gridManager.GetEventTileAtCell(targetCell)))
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

                    StartMoveProcess(targetPos, blockUntilComplete, () => MoveNextPathStep(token));
                }
            );
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

        /// <summary>
        ///     判断格子是否可直接通行（空地且非障碍/事件）。
        /// </summary>
        /// <param name="cellPos">格子坐标。</param>
        /// <returns>是否可直接通行。</returns>
        private bool IsCellDirectlyPassable(Vector3Int cellPos)
        {
            if (!_gridManager) return false;
            return IsCellBasicPassable(cellPos) && !_gridManager.GetEventTileAtCell(cellPos);
        }

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
        ///     判断是否需要忽略本次移动输入。
        /// </summary>
        /// <param name="args">玩家输入参数。</param>
        /// <returns>若应忽略移动输入则返回 true；否则返回 false。</returns>
        private bool ShouldIgnoreMoveInput(PlayerInputEventArgs args)
        {
            // 等待事件执行期间或对话激活时禁止移动输入
            if (_waitingForEventExecution) return true;
            if (_globalEventVariables.GetBool(GlobalEventKey.DialogueIsActive)) return true;
            // 移动中或输入无效时忽略
            return _isMoving || !args.IsValidInput || args.MoveDirection == Vector2.zero;
        }

        /// <summary>
        ///     尝试获取玩家当前所在的格子坐标。
        /// </summary>
        /// <param name="currentCell">输出的当前格子坐标。</param>
        /// <returns>转换成功返回 true，否则返回 false。</returns>
        private bool TryGetCurrentCell(out Vector3Int currentCell)
        {
            currentCell = Vector3Int.zero;
            if (!_gridManager) return false;
            // 统一通过 GridManager 进行世界坐标到格子坐标的转换
            return _gridManager.TryWorldToCellPos(transform.position, out currentCell);
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

        #endregion
    }
}