using System;
using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayAnimationAction", menuName = "EventNodes/Action/PlayAnimation")]
public class PlayAnimationAction : ActionNode
{
    [Tooltip("Animator 状态名（用于直接 Play 或用于 PollStateEnd 检测）")]
    public string stateName;

    [Tooltip("Animator Trigger 参数名（若使用 Trigger 则填写）")]
    public string triggerParameter;

    public enum PlayMode { Trigger, PlayState }
    [Tooltip("Trigger：调用 SetTrigger(triggerParameter)。PlayState：调用 animator.Play(stateName)")]
    public PlayMode playMode = PlayMode.Trigger;

    [Tooltip("是否等待动画完成才结束此节点（true 会阻塞后续节点直到动作完成）")]
    public bool waitForCompletion = true;

    [Tooltip("等待策略：优先尝试用 ClipLength，再回退到 PollStateEnd；若都失败则使用 fallbackTimeout")]
    public bool tryUseClipLength = true;

    [Tooltip("如果无法通过 clip length 或状态监测确定结束，使用该超时作为后备（秒）")]
    public float fallbackTimeout = 1.0f;

    public override void Execute(EventNodeContext ctx, Action onComplete)
    {
        if (ctx == null)
        {
            onComplete?.Invoke();
            return;
        }

        // 查找 Animator：优先 ctx.TileObject，再查所有子对象
        Animator animator = null;
        if (ctx.TileObject != null)
        {
            animator = ctx.TileObject.GetComponentInChildren<Animator>();
        }

        if (animator == null)
        {
            Debug.LogWarning($"PlayAnimationAction: 未找到 Animator 于 TileObject（cell={ctx.CellPos}）");
            onComplete?.Invoke();
            return;
        }

        if (!waitForCompletion)
        {
            // 非阻塞：立即触发动画然后完成
            TryTriggerOrPlay(animator);
            onComplete?.Invoke();
            return;
        }

        // 阻塞路径：需要异步等待动画完成
        if (ctx.OwnerMono == null)
        {
            // 无法启动协程：退回到非阻塞行为
            Debug.LogWarning("PlayAnimationAction: OwnerMono 为 null，无法等待动画，改为非阻塞执行");
            TryTriggerOrPlay(animator);
            onComplete?.Invoke();
            return;
        }

        // 启动协程在 OwnerMono 上等待动画完成
        ctx.OwnerMono.StartCoroutine(PlayAndWaitCoroutine(animator, ctx, onComplete));
    }

    private void TryTriggerOrPlay(Animator animator)
    {
        try
        {
            if (playMode == PlayMode.Trigger && !string.IsNullOrEmpty(triggerParameter))
            {
                animator.SetTrigger(triggerParameter);
            }
            else if (!string.IsNullOrEmpty(stateName))
            {
                animator.Play(stateName);
            }
            else
            {
                Debug.LogWarning("PlayAnimationAction: 未指定 triggerParameter 或 stateName");
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    private IEnumerator PlayAndWaitCoroutine(Animator animator, EventNodeContext ctx, Action onComplete)
    {
        // 先触发播放
        TryTriggerOrPlay(animator);

        float elapsed = 0f;

        // 优先通过 clip length 计算（如果启用）
        if (tryUseClipLength && !string.IsNullOrEmpty(stateName) && animator.runtimeAnimatorController != null)
        {
            // 尝试查找同名 clip
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip == null) continue;
                if (string.Equals(clip.name, stateName, StringComparison.OrdinalIgnoreCase))
                {
                    float waitSec = clip.length;
                    // 考虑 animator.speed
                    float speed = Mathf.Approximately(animator.speed, 0f) ? 1f : animator.speed;
                    waitSec = waitSec / speed;
                    // 防止零或极小时间
                    if (waitSec <= 0f) waitSec = fallbackTimeout;
                    yield return new WaitForSeconds(waitSec);
                    onComplete?.Invoke();
                    yield break;
                }
            }
        }

        // 其次尝试轮询 Animator 状态（若 stateName 给定）
        if (!string.IsNullOrEmpty(stateName))
        {
            // 等待状态进入
            float waitEnterTimeout = Mathf.Max(0.5f, fallbackTimeout);
            float timer = 0f;
            bool entered = false;
            while (timer < waitEnterTimeout)
            {
                var info = animator.GetCurrentAnimatorStateInfo(0);
                if (info.IsName(stateName))
                {
                    entered = true;
                    break;
                }
                timer += Time.deltaTime;
                yield return null;
            }

            if (entered)
            {
                // 等待 normalizedTime >= 1
                while (true)
                {
                    var info = animator.GetCurrentAnimatorStateInfo(0);
                    if (info.IsName(stateName) && info.normalizedTime >= 1f)
                    {
                        onComplete?.Invoke();
                        yield break;
                    }
                    // safety timeout
                    elapsed += Time.deltaTime;
                    if (elapsed > Mathf.Max(1f, fallbackTimeout * 5f))
                    {
                        Debug.LogWarning($"PlayAnimationAction: 等待状态 {stateName} 超时，使用回退超时 {fallbackTimeout}s");
                        yield return new WaitForSeconds(fallbackTimeout);
                        onComplete?.Invoke();
                        yield break;
                    }
                    yield return null;
                }
            }
        }

        // 最后回退：等待 fallbackTimeout 秒
        yield return new WaitForSeconds(fallbackTimeout);
        onComplete?.Invoke();
    }
}