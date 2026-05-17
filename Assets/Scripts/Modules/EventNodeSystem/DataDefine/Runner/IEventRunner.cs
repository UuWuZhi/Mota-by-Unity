using System;
using System.Collections;
using Modules.EventNodeSystem.DataDefine.Context;

namespace Modules.EventNodeSystem.DataDefine.Runner
{
    public interface IEventRunner
    {
        /// <summary>
        ///     启动事件序列执行（非阻塞）
        /// </summary>
        /// <param name="sequence">事件序列。</param>
        /// <param name="ctx">事件上下文。</param>
        /// <param name="onComplete">完成回调。</param>
        void StartSequence(EventSequence sequence, EventNodeContext ctx, Action onComplete);

        /// <summary>
        ///     执行事件序列并返回可等待的协程
        /// </summary>
        /// <param name="sequence">事件序列。</param>
        /// <param name="ctx">事件上下文。</param>
        /// <param name="onComplete">完成回调。</param>
        /// <returns>可等待的协程。</returns>
        IEnumerator RunSequenceAndWait(EventSequence sequence, EventNodeContext ctx, Action onComplete);
    }
}