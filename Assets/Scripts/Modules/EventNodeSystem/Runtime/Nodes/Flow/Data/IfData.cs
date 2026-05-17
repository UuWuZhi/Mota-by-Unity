using System;
using Modules.EventNodeSystem.DataDefine;
using Modules.EventNodeSystem.Runtime.Nodes.Condition;

namespace Modules.EventNodeSystem.Runtime.Nodes.Flow.Data
{
    /// <summary>
    ///     已弃用的 If 数据结构：仅保留用于旧资产兼容与迁移提示。
    /// </summary>
    [Serializable]
    [Obsolete("If 数据结构已弃用，请使用 Jump + 单条件节点的平铺结构替代。")]
    public class IfData : BaseNodeData
    {
        /// <summary>
        ///     条件节点资产。
        /// </summary>
        public ConditionNode condition;

        /// <summary>
        ///     命中时跳转的标签名。
        /// </summary>
        public string trueLabelName;

        /// <summary>
        ///     未命中时跳转的标签名。
        /// </summary>
        public string falseLabelName;

        public override string GetSummary()
        {
            var name = condition != null ? condition.name : "None";
            return $"▶ [If/Deprecated] {name}";
        }
    }
}
