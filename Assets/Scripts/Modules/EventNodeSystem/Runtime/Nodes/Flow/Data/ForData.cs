using System;
using Modules.EventNodeSystem.DataDefine;
using Modules.EventNodeSystem.DataDefine.Context;

namespace Modules.EventNodeSystem.Runtime.Nodes.Flow.Data
{
    /// <summary>
    ///     已弃用的 For 数据结构：仅保留用于旧资产兼容与迁移提示。
    /// </summary>
    [Serializable]
    [Obsolete("For 数据结构已弃用，请使用 Jump + 单条件节点的平铺结构替代。")]
    public class ForData : BaseNodeData
    {
        /// <summary>
        ///     循环计数来源键。
        /// </summary>
        public ContextVarKey countVarKey = ContextVarKey.UseCount;

        /// <summary>
        ///     循环起始标签名。
        /// </summary>
        public string beginLabelName;

        /// <summary>
        ///     循环结束标签名。
        /// </summary>
        public string endLabelName;

        public override string GetSummary()
        {
            return $"▶ [For/Deprecated] {beginLabelName} -> {endLabelName}";
        }
    }
}
