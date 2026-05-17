using System;
using System.Collections;
using Modules.EventNodeSystem.DataDefine;
using Modules.EventNodeSystem.DataDefine.Context;
using Modules.EventNodeSystem.DataDefine.Runner;
using Modules.EventNodeSystem.Runtime.Nodes.Action.Data;
using UnityEngine;

namespace Modules.EventNodeSystem.Runtime.Nodes.Action.TileAction
{
    [CreateAssetMenu(fileName = "PlayAnimation", menuName = "EventNodes/Action/PlayAnimation")]
    public class PlayAnimation : TileActionNode
    {
        public override RunnerExecutionHint GetExecutionHint()
        {
            return RunnerExecutionHint.AsyncBlocking;
        }

        public override void ExecuteTile(BaseNodeData data, EventNodeTileContext ctx, System.Action onComplete)
        {
            if (data is not PlayAnimationData playData)
            {
                Debug.LogWarning("PlayAnimation: data 类型不匹配，跳过执行。");
                onComplete?.Invoke();
                return;
            }

            if (ctx == null)
            {
                onComplete?.Invoke();
                return;
            }

            // 查找 Animator：优先 ctx.TileObject，再查所有子对象
            Animator animator = null;
            if (ctx.TileObject) animator = ctx.TileObject.GetComponentInChildren<Animator>();

            if (!animator)
            {
                Debug.LogWarning($"PlayAnimation: 未找到 Animator 于 TileObject（cell={ctx.CellPos}）");
                onComplete?.Invoke();
                return;
            }

            if (!playData.waitForCompletion)
            {
                // 非阻塞：立即触发动画然后完成
                TryTriggerOrPlay(animator, playData);
                onComplete?.Invoke();
                return;
            }

            // 阻塞路径：需要异步等待动画完成
            if (!ctx.OwnerMono)
            {
                // 无法启动协程：退回到非阻塞行为
                Debug.LogWarning("PlayAnimation: OwnerMono 为 null，无法等待动画，改为非阻塞执行");
                TryTriggerOrPlay(animator, playData);
                onComplete?.Invoke();
                return;
            }

            // 启动协程在 OwnerMono 上等待动画完成
            ctx.OwnerMono.StartCoroutine(PlayAndWaitCoroutine(animator, playData, onComplete));
        }

        private void TryTriggerOrPlay(Animator animator, PlayAnimationData data)
        {
            try
            {
                if (data.playMode == PlayAnimationData.PlayMode.Trigger && !string.IsNullOrEmpty(data.triggerParameter))
                    animator.SetTrigger(data.triggerParameter);
                else if (!string.IsNullOrEmpty(data.stateName))
                    animator.Play(data.stateName);
                else
                    Debug.LogWarning("PlayAnimation: 未指定 triggerParameter 或 stateName");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private IEnumerator PlayAndWaitCoroutine(Animator animator, PlayAnimationData data,
            System.Action onComplete)
        {
            // 先触发播放
            TryTriggerOrPlay(animator, data);

            var elapsed = 0f;

            // 优先通过 clip length 计算（如果启用）
            if (data.tryUseClipLength && !string.IsNullOrEmpty(data.stateName) &&
                animator.runtimeAnimatorController)
                // 尝试查找同名 clip
                foreach (var clip in animator.runtimeAnimatorController.animationClips)
                {
                    if (!clip) continue;
                    if (!string.Equals(clip.name, data.stateName, StringComparison.OrdinalIgnoreCase)) continue;
                    var waitSec = clip.length;
                    // 考虑 animator.speed
                    var speed = Mathf.Approximately(animator.speed, 0f) ? 1f : animator.speed;
                    waitSec = waitSec / speed;
                    // 防止零或极小时间
                    if (waitSec <= 0f) waitSec = data.fallbackTimeout;
                    yield return new WaitForSeconds(waitSec);
                    onComplete?.Invoke();
                    yield break;
                }

            // 其次尝试轮询 Animator 状态（若 stateName 给定）
            if (!string.IsNullOrEmpty(data.stateName))
            {
                // 等待状态进入
                var waitEnterTimeout = Mathf.Max(0.5f, data.fallbackTimeout);
                var timer = 0f;
                var entered = false;
                while (timer < waitEnterTimeout)
                {
                    var info = animator.GetCurrentAnimatorStateInfo(0);
                    if (info.IsName(data.stateName))
                    {
                        entered = true;
                        break;
                    }

                    timer += Time.deltaTime;
                    yield return null;
                }

                if (entered)
                    // 等待 normalizedTime >= 1
                    while (true)
                    {
                        var info = animator.GetCurrentAnimatorStateInfo(0);
                        if (info.IsName(data.stateName) && info.normalizedTime >= 1f)
                        {
                            onComplete?.Invoke();
                            yield break;
                        }

                        // safety timeout
                        elapsed += Time.deltaTime;
                        if (elapsed > Mathf.Max(1f, data.fallbackTimeout * 5f))
                        {
                            Debug.LogWarning($"PlayAnimation: 等待状态 {data.stateName} 超时，使用回退超时 {data.fallbackTimeout}s");
                            yield return new WaitForSeconds(data.fallbackTimeout);
                            onComplete?.Invoke();
                            yield break;
                        }

                        yield return null;
                    }
            }

            // 最后回退：等待 fallbackTimeout 秒
            yield return new WaitForSeconds(data.fallbackTimeout);
            onComplete?.Invoke();
        }
    }
}