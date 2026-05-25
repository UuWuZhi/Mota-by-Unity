using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Modules.Core.Runtime;
using Modules.EventNodeSystem.DataDefine;
using Modules.EventNodeSystem.DataDefine.Context;
using Modules.EventNodeSystem.DataDefine.Runner;
using Modules.EventNodeSystem.Runtime.Nodes;
using Modules.EventNodeSystem.Runtime.Nodes.Condition;
using Modules.EventNodeSystem.Runtime.Nodes.Flow.Data;
using Modules.Map.Runtime;
using Modules.Player.DataDefine;
using Modules.Player.Runtime.Attribute;
using UnityEngine;
using VContainer;

namespace Modules.EventNodeSystem.Runtime.Runner
{
    /// <summary>
    ///     事件执行服务：负责注入依赖并驱动节点执行。
    /// </summary>
    public class EventRunnerService : IEventRunner
    {
        #region 内部类型

        /// <summary>
        ///     Runner 全局状态（仅表达是否存在可执行任务）。
        /// </summary>
        private enum RunnerState
        {
            Idle,
            Busy
        }

        /// <summary>
        ///     单条节点链任务状态。
        /// </summary>
        private enum SequenceRunState
        {
            Ready,
            Running,
            Blocked,
            Completed,
            Aborted
        }

        /// <summary>
        ///     单次事件序列执行请求（任务级状态载体）。
        /// </summary>
        private sealed class SequenceRunRequest
        {
            /// <summary>
            ///     单帧内各指令索引的访问次数统计。
            /// </summary>
            public readonly Dictionary<int, int> IndexVisitCounter = new();

            /// <summary>
            ///     任务内标签名到指令索引的映射。
            /// </summary>
            public readonly Dictionary<string, int> LabelMap = new();

            /// <summary>
            ///     本次执行所使用的事件上下文。
            /// </summary>
            public EventNodeContext Context;

            /// <summary>
            ///     当前所在帧号，用于死循环防护统计。
            /// </summary>
            public int CurrentFrame;

            /// <summary>
            ///     当前执行到的指令索引。
            /// </summary>
            public int CurrentIndex;

            /// <summary>
            ///     任务完成时触发的回调。
            /// </summary>
            public Action OnComplete;

            /// <summary>
            ///     本次执行请求的唯一标识。
            /// </summary>
            public long RunId;

            /// <summary>
            ///     事件序列数据源。
            /// </summary>
            public EventSequence Sequence;

            /// <summary>
            ///     请求创建时的帧号。
            /// </summary>
            public int StartFrame;

            /// <summary>
            ///     当前任务状态。
            /// </summary>
            public SequenceRunState State;

            /// <summary>
            ///     任务级执行步数计数器。
            /// </summary>
            public int StepCounter;

            /// <summary>
            ///     是否启用了 SyncImmediate 轻量防呆。
            /// </summary>
            public bool SyncGuardEnabled;

            /// <summary>
            ///     当前 SyncImmediate 防呆对应的指令索引。
            /// </summary>
            public int SyncGuardIndex;

            /// <summary>
            ///     当前 SyncImmediate 防呆对应的节点类型名。
            /// </summary>
            public string SyncGuardNodeType;

            /// <summary>
            ///     当前 SyncImmediate 防呆开始时间。
            /// </summary>
            public float SyncGuardStartTime;
        }

        #endregion

        #region 依赖服务

        /// <summary>
        ///     协程宿主，用于驱动需要 MonoBehaviour 的异步逻辑。
        /// </summary>
        private readonly CoroutineRunner _coroutineRunner;

        /// <summary>
        ///     网格管理器。
        /// </summary>
        private readonly GridManager _gridManager;

        /// <summary>
        ///     背包服务。
        /// </summary>
        private readonly IInventoryService _inventoryService;

        /// <summary>
        ///     玩家属性服务。
        /// </summary>
        private readonly PlayerAttribute _playerAttribute;

        /// <summary>
        ///     事件中心。
        /// </summary>
        private readonly EventCenter _eventCenter;

        /// <summary>
        ///     地图管理器。
        /// </summary>
        private readonly MapManager _mapManager;

        /// <summary>
        ///     容器解析器。
        /// </summary>
        private readonly IObjectResolver _resolver;

        /// <summary>
        ///     Data 到节点模板的映射注册表。
        /// </summary>
        private readonly EventNodeSystemRegistry _registry;

        #endregion

        #region 配置常量

        /// <summary>
        ///     单条任务最大执行步数阈值（死循环防护）。
        /// </summary>
        private const int MaxStepsPerSequence = 10000;

        /// <summary>
        ///     单帧内同一索引最大访问次数阈值（死循环防护）。
        /// </summary>
        private const int MaxVisitsPerFramePerIndex = 64;

        /// <summary>
        ///     就绪队列最大容量。
        /// </summary>
        private const int MaxReadyQueueLength = 128;

        /// <summary>
        ///     节点声明为 SyncImmediate 时的轻量告警超时（秒）。
        /// </summary>
        private const float SyncImmediateWarnTimeoutSeconds = 0.2f;

        #endregion

        #region 运行时状态

        /// <summary>
        ///     当前活动任务（单执行位）。
        /// </summary>
        private SequenceRunRequest _activeRun;

        /// <summary>
        ///     就绪任务队列（FIFO）。
        /// </summary>
        private readonly Queue<SequenceRunRequest> _readyQueue = new();

        /// <summary>
        ///     阻塞任务容器（runId -> task）。
        /// </summary>
        private readonly Dictionary<long, SequenceRunRequest> _blockedRuns = new();

        /// <summary>
        ///     下一个任务 RunId。
        /// </summary>
        private long _nextRunId = 1;

        /// <summary>
        ///     Runner 全局状态。
        /// </summary>
        private RunnerState _runnerState = RunnerState.Idle;

        #endregion

        #region 构造与初始化

        /// <summary>
        ///     构造函数：解析运行依赖并初始化映射表。
        /// </summary>
        /// <param name="resolver">容器解析器。</param>
        [Inject]
        public EventRunnerService(IObjectResolver resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _coroutineRunner = resolver.Resolve<CoroutineRunner>();
            _gridManager = resolver.Resolve<GridManager>();
            _inventoryService = resolver.Resolve<IInventoryService>();
            _playerAttribute = resolver.Resolve<PlayerAttribute>();
            _eventCenter = resolver.Resolve<EventCenter>();
            _mapManager = resolver.Resolve<MapManager>();
            _registry = resolver.Resolve<EventNodeSystemRegistry>();
            EnsureRegistryLoaded();
        }

        /// <summary>
        ///     从 Resources 加载节点映射表到注册表。
        /// </summary>
        private void EnsureRegistryLoaded()
        {
            if (_registry == null) return;
            if (!_registry.TryLoadFromResources())
                DebugEditor.LogWarning("EventRunnerService: 未找到 NodeMappingTable 资源，无法加载映射表。请先生成资产。");
        }

        #endregion

        #region 公共入口

        /// <summary>
        ///     启动一条事件序列。
        ///     会创建任务对象并进入就绪队列，由调度器决定何时执行。
        /// </summary>
        /// <param name="sequence">事件序列。</param>
        /// <param name="ctx">事件上下文。</param>
        /// <param name="onComplete">任务完成回调。</param>
        public void StartSequence(EventSequence sequence, EventNodeContext ctx, Action onComplete)
        {
            if (sequence?.commands == null || sequence.commands.Count == 0)
            {
                DebugEditor.LogWarning("[EventRunnerService]:事件队列为空，直接完成");
                onComplete?.Invoke();
                return;
            }

            ctx ??= new EventNodeContext();
            ctx.OwnerMono = _coroutineRunner;

            var request = new SequenceRunRequest
            {
                Sequence = sequence,
                Context = ctx,
                OnComplete = onComplete,
                RunId = _nextRunId++,
                StartFrame = Time.frameCount,
                State = SequenceRunState.Ready,
                CurrentIndex = 0,
                CurrentFrame = Time.frameCount
            };

            // 新任务统一先入就绪队列，再由调度器拉起，避免直接覆盖活动任务状态。
            EnqueueRun(request);
            TryDequeueAndRunNext();
        }

        /// <summary>
        ///     通过 Runner 的协程宿主执行协程。
        /// </summary>
        /// <param name="routine">协程体。</param>
        /// <returns>协程句柄。</returns>
        public Coroutine RunCoroutine(IEnumerator routine)
        {
            return !_coroutineRunner ? null : _coroutineRunner.Run(routine);
        }

        /// <summary>
        ///     启动并等待一条事件序列执行完成。
        /// </summary>
        /// <param name="sequence">事件序列。</param>
        /// <param name="ctx">事件上下文。</param>
        /// <param name="onComplete">完成回调。</param>
        /// <returns>可等待的协程。</returns>
        public IEnumerator RunSequenceAndWait(EventSequence sequence, EventNodeContext ctx, Action onComplete)
        {
            if (sequence?.commands == null || sequence.commands.Count == 0)
            {
                DebugEditor.LogWarning("[EventRunnerService]:事件队列为空，直接完成");
                onComplete?.Invoke();
                yield break;
            }

            var finished = false;
            StartSequence(sequence, ctx, () =>
            {
                finished = true;
                onComplete?.Invoke();
            });

            while (!finished) yield return null;
        }

        #endregion

        #region 调度与主循环

        /// <summary>
        ///     入就绪队列。
        /// </summary>
        /// <param name="request">待入队任务。</param>
        private void EnqueueRun(SequenceRunRequest request)
        {
            if (_readyQueue.Count >= MaxReadyQueueLength)
            {
                DebugEditor.LogWarning(
                    $"[EventRunnerService]: 就绪队列已满({MaxReadyQueueLength})，丢弃新请求 runId={request.RunId}");
                request.OnComplete?.Invoke();
                return;
            }

            request.State = SequenceRunState.Ready;
            // FIFO：保证先入先出，避免饥饿。
            _readyQueue.Enqueue(request);
            UpdateRunnerState();
        }

        /// <summary>
        ///     若当前无活动任务，尝试从就绪队列拉起下一条任务。
        /// </summary>
        private void TryDequeueAndRunNext()
        {
            if (_activeRun != null || _readyQueue.Count == 0)
            {
                UpdateRunnerState();
                return;
            }

            // 从就绪队列取下一条任务进入执行位。
            var next = _readyQueue.Dequeue();
            ActivateRun(next);
        }

        /// <summary>
        ///     激活一条任务并开始执行。
        /// </summary>
        /// <param name="request">任务对象。</param>
        private void ActivateRun(SequenceRunRequest request)
        {
            _activeRun = request;
            _activeRun.State = SequenceRunState.Running;
            _activeRun.Context?.RegisterService(this);
            BuildLabelMap(_activeRun);
            UpdateRunnerState();
            ExecuteCurrentCommand();
        }

        /// <summary>
        ///     执行当前活动任务的当前指令。
        /// </summary>
        private void ExecuteCurrentCommand()
        {
            var run = _activeRun;
            if (run == null)
            {
                // 无活动任务：尝试拉起下一条就绪任务。
                TryDequeueAndRunNext();
                return;
            }

            if (run.Sequence?.commands == null)
            {
                // 任务数据异常或已被清空：按正常结束收口，避免悬挂。
                CompleteActiveRun(false);
                return;
            }

            if (run.CurrentIndex < 0 || run.CurrentIndex >= run.Sequence.commands.Count)
            {
                // 指令指针越界代表任务自然结束。
                CompleteActiveRun(false);
                return;
            }

            if (!TryPassDeadlockGuard(run, run.CurrentIndex))
            {
                AbortSequenceByDeadlock(run, "检测到无进展循环或超出执行阈值，已中断序列执行。");
                return;
            }

            var data = run.Sequence.commands[run.CurrentIndex];
            switch (data)
            {
                case null:
                    // 空指令安全跳过，继续推进。
                    AdvanceAndContinue();
                    return;
                case JumpData jumpData:
                    ExecuteJumpInstruction(run, jumpData, run.CurrentIndex);
                    return;
            }

            var node = _registry?.GetNode(data.GetType());
            if (!node)
            {
                // 无映射节点：记录告警并跳过，避免整条任务中断。
                DebugEditor.LogWarning($"EventRunnerService: 未找到节点模板，Data = {data.GetType().Name}");
                AdvanceAndContinue();
                return;
            }

            RegisterRequiredServices(node.GetRequiredServices(), run.Context, true);
            run.Context?.RegisterService(this);

            var executionHint = GetExecutionHint(node);
            if (executionHint == RunnerExecutionHint.SyncImmediate)
                // 节点声明同步：预期同调用栈完成，开启轻量防呆计时。
                BeginSyncImmediateGuard(run, run.CurrentIndex, node.GetType().Name);
            else
                // 节点声明异步：不做同步防呆。
                ClearSyncImmediateGuard(run);

            var runId = run.RunId;

            if (executionHint == RunnerExecutionHint.AsyncBlocking)
            {
                // 核心并发逻辑：
                // 1) 当前任务转 Blocked 并移入阻塞容器；
                // 2) 释放活动执行位；
                // 3) 立即调度下一条就绪任务。
                run.State = SequenceRunState.Blocked;
                _blockedRuns[runId] = run;
                _activeRun = null;
                UpdateRunnerState();

                try
                {
                    node.Execute(data, run.Context, () => OnNodeComplete(runId));
                }
                catch (Exception ex)
                {
                    DebugEditor.LogException(ex);
                    RecoverBlockedRunOnException(runId);
                }

                // 当前任务已阻塞，Runner 可继续执行其他链路。
                TryDequeueAndRunNext();
                return;
            }

            try
            {
                node.Execute(data, run.Context, () => OnNodeComplete(runId));
                CheckSyncImmediateGuardAfterExecute(run);
            }
            catch (Exception ex)
            {
                DebugEditor.LogException(ex);
                ClearSyncImmediateGuard(run);
                AdvanceAndContinue();
            }
        }

        /// <summary>
        ///     节点完成统一回调入口。
        ///     依据 runId 判断回调归属（阻塞恢复 / 活动推进 / 过期丢弃）。
        /// </summary>
        /// <param name="runId">任务标识。</param>
        private void OnNodeComplete(long runId)
        {
            // 1) 阻塞任务回归：异步回调到达后从 Blocked -> Ready。
            if (_blockedRuns.Remove(runId, out var blockedRun))
            {
                ClearSyncImmediateGuard(blockedRun);

                // 关键修复：异步节点完成后应推进到下一条指令。
                // 若不推进，将重复执行同一 AsyncBlocking 节点并触发死循环防护。
                blockedRun.CurrentIndex++;
                // 回归就绪前清理当前帧访问计数，避免历史计数误伤。
                blockedRun.CurrentFrame = Time.frameCount;
                blockedRun.IndexVisitCounter.Clear();

                // 回到就绪队列，等待调度器拉起继续执行。
                EnqueueRun(blockedRun);
                TryDequeueAndRunNext();
                return;
            }

            // 2) 正常推进：回调属于当前活动任务。
            if (_activeRun == null || _activeRun.RunId != runId)
            {
                // 3) 回调既不在阻塞容器，也不属于当前活动任务：视为过期回调。
                DebugEditor.LogWarning($"EventRunnerService: 丢弃过期回调 runId={runId}");
                return;
            }

            CheckSyncImmediateGuardOnComplete(_activeRun);
            AdvanceAndContinue();
        }

        /// <summary>
        ///     推进活动任务指针并继续执行下一条指令。
        /// </summary>
        private void AdvanceAndContinue()
        {
            if (_activeRun == null)
            {
                DebugEditor.LogWarning("[EventRunnerService]:无活动任务，无法推进指令。");
                return;
            }

            _activeRun.CurrentIndex++;
            ExecuteCurrentCommand();
        }

        /// <summary>
        ///     结束当前活动任务（正常或中断）。
        /// </summary>
        /// <param name="aborted">是否为中断结束。</param>
        private void CompleteActiveRun(bool aborted)
        {
            var run = _activeRun;
            if (run == null)
            {
                DebugEditor.LogWarning("[EventRunnerService]:无活动任务，无法完成。");
                TryDequeueAndRunNext();
                return;
            }

            _activeRun = null;
            run.State = aborted ? SequenceRunState.Aborted : SequenceRunState.Completed;
            ClearSyncImmediateGuard(run);

            try
            {
                run.OnComplete?.Invoke();
            }
            catch (Exception ex)
            {
                DebugEditor.LogException(ex);
            }

            UpdateRunnerState();
            TryDequeueAndRunNext();
        }

        /// <summary>
        ///     阻塞任务在执行异常时的恢复/收口逻辑。
        /// </summary>
        /// <param name="runId">任务标识。</param>
        private void RecoverBlockedRunOnException(long runId)
        {
            if (!_blockedRuns.Remove(runId, out var run))
            {
                DebugEditor.LogWarning($"EventRunnerService: 无法恢复异常任务，未找到 runId={runId}");
                return;
            }

            run.State = SequenceRunState.Aborted;
            ClearSyncImmediateGuard(run);
            try
            {
                run.OnComplete?.Invoke();
            }
            catch (Exception ex)
            {
                DebugEditor.LogException(ex);
            }

            UpdateRunnerState();
            TryDequeueAndRunNext();
        }

        /// <summary>
        ///     更新 Runner 全局状态（Idle/Busy）。
        /// </summary>
        private void UpdateRunnerState()
        {
            var hasWork = _activeRun != null || _readyQueue.Count > 0 || _blockedRuns.Count > 0;
            _runnerState = hasWork ? RunnerState.Busy : RunnerState.Idle;
        }

        #endregion

        #region 预扫描与跳转

        /// <summary>
        ///     构建任务内标签索引表。
        /// </summary>
        /// <param name="run">任务对象。</param>
        private void BuildLabelMap(SequenceRunRequest run)
        {
            run.LabelMap.Clear();
            if (run.Sequence?.commands == null) return;

            for (var i = 0; i < run.Sequence.commands.Count; i++)
                switch (run.Sequence.commands[i])
                {
                    case LabelData labelData when !string.IsNullOrEmpty(labelData.labelName):
                        // 检测重名：保留当前的覆盖行为，但记录警告以便定位问题。
                        if (run.LabelMap.TryGetValue(labelData.labelName, out var existingIndex))
                            DebugEditor.LogWarning(
                                $"EventRunnerService: 标签名重复 '{labelData.labelName}'，原索引={existingIndex}，覆盖为索引={i}。请检查序列以避免歧义。");
                        run.LabelMap[labelData.labelName] = i;
                        break;
                }
        }

        /// <summary>
        ///     将指定标签名称与索引值添加到映射表中，如果该标签名称尚未存在于映射表中。
        /// </summary>
        /// <remarks>如果标签名称已存在于映射表中，则不会进行任何操作。该方法不会覆盖现有的标签索引。</remarks>
        /// <param name="map">要更新的标签名称到索引的映射表。不能为空。</param>
        /// <param name="labelName">要添加到映射表的标签名称。不能为空或空字符串。</param>
        /// <param name="index">与标签名称关联的索引值。</param>
        private static void CacheLabelIndex(Dictionary<string, int> map, string labelName, int index)
        {
            if (string.IsNullOrEmpty(labelName)) return;
            map.TryAdd(labelName, index);
        }

        /// <summary>
        ///     按标签名跳转（作用于当前活动任务）。
        /// </summary>
        /// <param name="labelName">目标标签。</param>
        public void JumpToLabel(string labelName)
        {
            var run = _activeRun;
            if (run == null || string.IsNullOrEmpty(labelName)) return;

            if (run.LabelMap.TryGetValue(labelName, out var index))
                JumpToIndex(index);
            else
                DebugEditor.LogWarning($"EventRunnerService: 未找到标签 {labelName}");
        }

        /// <summary>
        ///     按索引跳转（作用于当前活动任务）。
        /// </summary>
        /// <param name="index">目标索引。</param>
        public void JumpToIndex(int index)
        {
            var run = _activeRun;
            if (run == null)
                // 保护：没有活动任务时无法跳转。
                return;
            run.CurrentIndex = index - 1;
        }

        /// <summary>
        ///     执行 Jump 指令，并尝试捕捉后一个条件节点作为判断依据。
        /// </summary>
        /// <param name="run">当前任务对象。</param>
        /// <param name="jumpData">Jump 数据。</param>
        /// <param name="jumpIndex">Jump 所在索引。</param>
        private void ExecuteJumpInstruction(SequenceRunRequest run, JumpData jumpData, int jumpIndex)
        {
            if (run == null || jumpData == null)
            {
                AdvanceAndContinue();
                return;
            }

            // 如果 Jump 标记为始终跳转，忽略后续条件直接跳转。
            if (jumpData.alwaysJump)
            {
                JumpToLabel(jumpData.targetLabelName);
                AdvanceAndContinue();
                return;
            }

            if (TryResolveAttachedCondition(run, jumpIndex, out var conditionData, out var conditionNode))
            {
                // 找到条件节点：注册所需服务并执行条件判断，依据结果选择性跳转或继续。
                RegisterRequiredServices(conditionNode.GetRequiredServices(), run.Context, true);
                run.Context?.RegisterService(this);

                try
                {
                    conditionNode.Evaluate(conditionData, run.Context, result =>
                    {
                        if (_activeRun == null || _activeRun.RunId != run.RunId)
                        {
                            DebugEditor.LogWarning($"EventRunnerService: 条件回调已过期 runId={run.RunId}");
                            return;
                        }

                        if (result)
                            JumpToLabel(jumpData.targetLabelName);
                        else
                            run.CurrentIndex = jumpIndex + 1;

                        AdvanceAndContinue();
                    });
                }
                catch (Exception ex)
                {
                    DebugEditor.LogException(ex);
                    DebugEditor.LogWarning(
                        $"EventRunnerService: 条件节点执行异常，已按无条件 Jump 处理。runId={run.RunId}; index={jumpIndex}; label={jumpData.targetLabelName}");
                    JumpToLabel(jumpData.targetLabelName);
                    AdvanceAndContinue();
                }

                return;
            }

            // 未找到条件节点：按无条件 Jump 处理。
            JumpToLabel(jumpData.targetLabelName);
            AdvanceAndContinue();
        }

        /// <summary>
        ///     尝试解析 Jump 后方紧邻的条件节点。
        /// </summary>
        /// <param name="run">当前任务对象。</param>
        /// <param name="jumpIndex">Jump 所在索引。</param>
        /// <param name="conditionData">条件数据输出。</param>
        /// <param name="conditionNode">条件节点模板输出。</param>
        /// <returns>是否成功解析到条件节点。</returns>
        private bool TryResolveAttachedCondition(SequenceRunRequest run, int jumpIndex, out BaseNodeData conditionData,
            out ConditionNode conditionNode)
        {
            conditionData = null;
            conditionNode = null;

            if (run?.Sequence?.commands == null) return false;

            var nextIndex = jumpIndex + 1;
            if (nextIndex < 0 || nextIndex >= run.Sequence.commands.Count) return false;

            conditionData = run.Sequence.commands[nextIndex];
            if (conditionData == null) return false;

            var nextNode = _registry?.GetNode(conditionData.GetType());
            if (nextNode is ConditionNode typedConditionNode)
            {
                conditionNode = typedConditionNode;
                return true;
            }

            DebugEditor.LogWarning(
                $"EventRunnerService: Jump 后继节点 {conditionData.GetType().Name} 不是条件节点，按无条件 Jump 处理。");
            return false;
        }

        #endregion

        #region 死循环防护

        /// <summary>
        ///     任务级死循环防护检查。
        /// </summary>
        /// <param name="run">任务对象。</param>
        /// <param name="index">当前索引。</param>
        /// <returns>是否通过检查。</returns>
        private bool TryPassDeadlockGuard(SequenceRunRequest run, int index)
        {
            run.StepCounter++;
            if (run.StepCounter > MaxStepsPerSequence)
                // 步数超过阈值，视为死循环。
                return false;

            var frame = Time.frameCount;
            if (frame != run.CurrentFrame)
            {
                run.CurrentFrame = frame;
                run.IndexVisitCounter.Clear();
            }

            var visits = run.IndexVisitCounter.GetValueOrDefault(index, 0);

            visits++;
            run.IndexVisitCounter[index] = visits;
            return visits <= MaxVisitsPerFramePerIndex;
        }

        /// <summary>
        ///     任务级死循环中断。
        /// </summary>
        /// <param name="run">任务对象。</param>
        /// <param name="reason">中断原因。</param>
        private void AbortSequenceByDeadlock(SequenceRunRequest run, string reason)
        {
            var labelName = ResolveLabelNameByIndex(run, run.CurrentIndex);
            var sequenceHash = run.Sequence?.GetHashCode() ?? 0;
            DebugEditor.LogError(
                $"[EventRunnerService]:检测到可能存在死循环，任务已中断 | reason={reason}; sequenceHash={sequenceHash}; runId={run.RunId}; index={run.CurrentIndex}; step={run.StepCounter}; frame={Time.frameCount}; label={labelName}");
            CompleteActiveRun(true);
        }

        /// <summary>
        ///     根据索引反查标签名（用于日志）。
        /// </summary>
        /// <param name="run">任务对象。</param>
        /// <param name="index">索引。</param>
        /// <returns>标签名或 N/A。</returns>
        private string ResolveLabelNameByIndex(SequenceRunRequest run, int index)
        {
            foreach (var pair in run.LabelMap.Where(pair => pair.Value == index))
                return pair.Key;

            return "N/A";
        }

        #endregion

        #region 服务注入与执行提示

        /// <summary>
        ///     按节点声明注册所需服务到上下文。
        /// </summary>
        /// <param name="required">所需服务类型集合。</param>
        /// <param name="ctx">上下文。</param>
        /// <param name="logRegistration">是否打印注册日志。</param>
        private void RegisterRequiredServices(IEnumerable<Type> required, EventNodeContext ctx,
            bool logRegistration = false)
        {
            if (required == null || ctx == null) return;
            foreach (var type in required)
                if (TryGetKnownService(type, out var service))
                {
                    if (logRegistration) DebugEditor.Log($"注册服务 {type.Name} 到上下文");
                    ctx.RegisterService(type, service);
                }
        }

        /// <summary>
        ///     获取已知服务或通过容器动态解析。
        /// </summary>
        /// <param name="type">服务类型。</param>
        /// <param name="service">解析结果。</param>
        /// <returns>是否成功解析。</returns>
        private bool TryGetKnownService(Type type, out object service)
        {
            service = null;
            if (type == null) return false;

            if (type == typeof(CoroutineRunner)) service = _coroutineRunner;
            else if (type == typeof(GridManager)) service = _gridManager;
            else if (type == typeof(IInventoryService)) service = _inventoryService;
            else if (type == typeof(PlayerAttribute)) service = _playerAttribute;
            else if (type == typeof(EventCenter)) service = _eventCenter;
            else if (type == typeof(MapManager)) service = _mapManager;
            else
                try
                {
                    service = _resolver.Resolve(type);
                }
                catch (Exception)
                {
                    DebugEditor.LogWarning($"EventRunnerService: 无法解析所需的服务类型 {type.Name}");
                    return false;
                }

            return service != null;
        }

        /// <summary>
        ///     获取节点执行提示。
        ///     未显式声明时默认按 AsyncBlocking 处理。
        /// </summary>
        /// <param name="node">节点模板。</param>
        /// <returns>执行提示。</returns>
        private RunnerExecutionHint GetExecutionHint(EventNode node)
        {
            if (node is IRunnerExecutionHintProvider provider) return provider.GetExecutionHint();

            return RunnerExecutionHint.AsyncBlocking;
        }

        #endregion

        #region SyncImmediate 防呆

        /// <summary>
        ///     开启 SyncImmediate 轻量防呆计时。
        /// </summary>
        /// <param name="run">任务对象。</param>
        /// <param name="index">当前索引。</param>
        /// <param name="nodeType">节点类型名。</param>
        private void BeginSyncImmediateGuard(SequenceRunRequest run, int index, string nodeType)
        {
            run.SyncGuardEnabled = true;
            run.SyncGuardIndex = index;
            run.SyncGuardNodeType = nodeType;
            run.SyncGuardStartTime = Time.realtimeSinceStartup;
        }

        /// <summary>
        ///     Execute 返回后检查同步节点是否疑似超时。
        /// </summary>
        /// <param name="run">任务对象。</param>
        private void CheckSyncImmediateGuardAfterExecute(SequenceRunRequest run)
        {
            if (!run.SyncGuardEnabled) return;
            var elapsed = Time.realtimeSinceStartup - run.SyncGuardStartTime;
            if (elapsed > SyncImmediateWarnTimeoutSeconds)
                DebugEditor.LogWarning(
                    $"[ENS-ExecutionHintGuard] nodeType={run.SyncGuardNodeType}; runId={run.RunId}; index={run.SyncGuardIndex}; hint=SyncImmediate; elapsedMs={elapsed * 1000f:F1}; frame={Time.frameCount}");
        }

        /// <summary>
        ///     回调完成时检查同步节点耗时并清理防呆状态。
        /// </summary>
        /// <param name="run">任务对象。</param>
        private void CheckSyncImmediateGuardOnComplete(SequenceRunRequest run)
        {
            if (!run.SyncGuardEnabled) return;
            var elapsed = Time.realtimeSinceStartup - run.SyncGuardStartTime;
            if (elapsed > SyncImmediateWarnTimeoutSeconds)
                DebugEditor.LogWarning(
                    $"[ENS-ExecutionHintGuard] nodeType={run.SyncGuardNodeType}; runId={run.RunId}; index={run.SyncGuardIndex}; hint=SyncImmediate; elapsedMs={elapsed * 1000f:F1}; frame={Time.frameCount}; stage=OnComplete");

            ClearSyncImmediateGuard(run);
        }

        /// <summary>
        ///     清理同步防呆状态。
        /// </summary>
        /// <param name="run">任务对象。</param>
        private void ClearSyncImmediateGuard(SequenceRunRequest run)
        {
            run.SyncGuardEnabled = false;
            run.SyncGuardIndex = -1;
            run.SyncGuardNodeType = null;
            run.SyncGuardStartTime = 0f;
        }

        #endregion
    }
}