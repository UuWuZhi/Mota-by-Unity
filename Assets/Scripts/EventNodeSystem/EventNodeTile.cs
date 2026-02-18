using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public enum EnterPermission         // 玩家进入权限：是否允许玩家进入格子，或者在事件执行后决定
    {
        Allow,                          // 允许进入
        Deny,                           // 不允许进入
        DecideAfterExecution            // 事件执行后决定（可能基于事件结果或玩家选择）
    }

    public enum ExecutionBlocking       // 事件执行期间是否阻塞玩家其他操作（如移动、交互等）
    {
        None,                           // 不阻塞
        BlockDuringExecution            // 在事件执行期间阻塞玩家其他操作
    }

    public TriggerMode triggerMode = TriggerMode.OnPlayerEnter;
    public EnterPermission enterPermission = EnterPermission.Allow;
    public ExecutionBlocking executionBlocking = ExecutionBlocking.None;

    public List<EventNode> actions = new List<EventNode>();

    [HideInInspector] public Vector3Int CellPos;

    [NonSerialized] private bool _isTriggering = false;
    private Coroutine _triggerTimeoutCoroutine = null;
    public float triggerTimeoutSeconds = 10f;

    public bool IsTriggering => _isTriggering;

    // Try to begin triggering. Returns true if this call acquired the triggering lock.
    public bool TryBeginTrigger(float timeoutSeconds = -1f)
    {
        if (_isTriggering) return false;
        _isTriggering = true;
        float t = timeoutSeconds > 0f ? timeoutSeconds : triggerTimeoutSeconds;
        if (t > 0f && this != null && gameObject != null)
        {
            // start timeout coroutine on this MonoBehaviour
            _triggerTimeoutCoroutine = StartCoroutine(TriggerTimeoutCoroutine(t));
        }
        return true;
    }

    public void EndTrigger()
    {
        _isTriggering = false;
        if (_triggerTimeoutCoroutine != null)
        {
            try { StopCoroutine(_triggerTimeoutCoroutine); } catch { }
            _triggerTimeoutCoroutine = null;
        }
    }

    private System.Collections.IEnumerator TriggerTimeoutCoroutine(float t)
    {
        yield return new WaitForSeconds(t);
        Debug.LogWarning($"EventNodeTile.TriggerTimeout: auto-clearing trigger flag for {name}");
        _isTriggering = false;
        _triggerTimeoutCoroutine = null;
    }

}