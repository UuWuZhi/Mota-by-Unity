using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using TriInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Modules.EventNodeSystem.DataDefine
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
        [SerializeReference, ListDrawerSettings(AlwaysExpanded = true)]
        public EventSequence sequence = new();
        /// <summary>
        ///     在 Inspector 中显示的按钮回调（通过 TriInspector 的 [Button] 特性）。
        ///     编辑器环境下使用反射调用 Editor 窗口的静态打开方法以避免在运行时代码中直接引用 Editor 程序集。
        /// </summary>
        [Button]
        public void OpenEventPage()
        {
#if UNITY_EDITOR
            try
            {
                // 通过反射查找编辑器窗口类型并调用 OpenFor(this)
                var editorType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => { try { return a.GetTypes(); } catch { return new Type[0]; } })
                    .FirstOrDefault(t => t.FullName == "Editor.EventPageEditorWindow");

                if (editorType == null)
                {
                    Debug.LogWarning($"[EventNodeTile(EventNodeTile)]: 未找到 EventPageEditorWindow 类型，无法打开事件页窗口。");
                    return;
                }

                var openMethod = editorType.GetMethod("OpenFor", BindingFlags.Public | BindingFlags.Static);
                if (openMethod == null)
                {
                    Debug.LogWarning($"[EventNodeTile(EventNodeTile)]: 在 {editorType.FullName} 中未找到 OpenFor 方法。");
                    return;
                }

                openMethod.Invoke(null, new object[] { this });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EventNodeTile(EventNodeTile)]: 调用事件页窗口失败：{ex}");
            }
#endif
        }
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