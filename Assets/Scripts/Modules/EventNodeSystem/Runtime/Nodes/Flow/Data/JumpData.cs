using System;
using Modules.EventNodeSystem.DataDefine;

namespace Modules.EventNodeSystem.Runtime.Nodes.Flow.Data
{
    [Serializable]
    public class JumpData : BaseNodeData
    {
        /// <summary>
        ///     目标标签名称。
        /// </summary>
        public string targetLabelName;

        /// <summary>
        ///     始终跳转标志：若为 true 则忽略后续条件节点，直接跳转。
        /// </summary>
        public bool alwaysJump = false;

        public override string GetSummary()
        {
            return $"▶ [跳转] {targetLabelName}" + (alwaysJump ? " (Always)" : string.Empty);
        }
    }
}
