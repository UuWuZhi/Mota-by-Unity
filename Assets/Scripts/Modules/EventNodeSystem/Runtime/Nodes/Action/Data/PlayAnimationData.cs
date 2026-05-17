using System;
using Modules.EventNodeSystem.DataDefine;

namespace Modules.EventNodeSystem.Runtime.Nodes.Action.Data
{
    [Serializable]
    public class PlayAnimationData : BaseNodeData
    {
        public enum PlayMode
        {
            Trigger,
            PlayState
        }

        /// <summary>
        ///     Animator 状态名。
        /// </summary>
        public string stateName;

        /// <summary>
        ///     Animator Trigger 参数名。
        /// </summary>
        public string triggerParameter;

        /// <summary>
        ///     播放方式。
        /// </summary>
        public PlayMode playMode = PlayMode.Trigger;

        /// <summary>
        ///     是否等待动画完成。
        /// </summary>
        public bool waitForCompletion = true;

        /// <summary>
        ///     是否优先使用 Clip 时长判断。
        /// </summary>
        public bool tryUseClipLength = true;

        /// <summary>
        ///     回退等待时间（秒）。
        /// </summary>
        public float fallbackTimeout = 1.0f;

        public override string GetSummary()
        {
            var label = !string.IsNullOrEmpty(stateName) ? stateName : triggerParameter;
            return $"◆ 播放动画: {label}";
        }
    }
}