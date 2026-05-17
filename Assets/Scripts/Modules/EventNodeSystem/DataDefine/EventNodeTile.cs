using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace Modules.EventNodeSystem.DataDefine.Data
{
    /// <summary>
    ///     事件挂载点
    /// </summary>
    public class EventNodeTile : MonoBehaviour
    {
        public enum EnterPermission // 玩家进入权限：是否允许玩家进入格子，或者在事件执行后决定
        {
            Allow, // 允许进入
            Deny, // 不允许进入
            DecideAfterExecution // 事件执行后决定（可能基于事件结果或玩家选择）
        }

        public enum ExecutionBlocking // 事件执行期间是否阻塞玩家其他操作（如移动、交互等）
        {
            None, // 不阻塞
            BlockDuringExecution // 在事件执行期间阻塞玩家其他操作
        }

        public enum TriggerMode // 事件触发方式
        {
            OnLoad, // 场景加载时触发
            OnPlayerEnter, // 玩家进入时触发
            OnPlayerArrived // 玩家到达时触发（进入格子后停止移动时触发）
        }

        public TriggerMode triggerMode = TriggerMode.OnPlayerEnter;
        public EnterPermission enterPermission = EnterPermission.Allow;
        public ExecutionBlocking executionBlocking = ExecutionBlocking.None;

        /// <summary>
        ///     事件序列数据容器。
        /// </summary>
        public EventSequence sequence = new();

        [FormerlySerializedAs("CellPos")] [HideInInspector]
        public Vector3Int cellPos;

        public float triggerTimeoutSeconds = 10f;

        private Coroutine _triggerTimeoutCoroutine;

        [field: NonSerialized] public bool IsTriggering { get; private set; }

        // Try to begin triggering. Returns true if this call acquired the triggering lock.
        public bool TryBeginTrigger(float timeoutSeconds = -1f)
        {
            if (IsTriggering) return false;
            IsTriggering = true;
            var t = timeoutSeconds > 0f ? timeoutSeconds : triggerTimeoutSeconds;
            if (t > 0f && this != null && gameObject != null)
                // start timeout coroutine on this MonoBehaviour
                _triggerTimeoutCoroutine = StartCoroutine(TriggerTimeoutCoroutine(t));
            return true;
        }

        public void EndTrigger()
        {
            IsTriggering = false;
            if (_triggerTimeoutCoroutine != null)
            {
                try
                {
                    StopCoroutine(_triggerTimeoutCoroutine);
                }
                catch
                {
                    // ignored
                }

                _triggerTimeoutCoroutine = null;
            }
        }

        private IEnumerator TriggerTimeoutCoroutine(float t)
        {
            yield return new WaitForSeconds(t);
            Debug.LogWarning($"EventNodeTile.TriggerTimeout: auto-clearing trigger flag for {name}");
            IsTriggering = false;
            _triggerTimeoutCoroutine = null;
        }
    }
}