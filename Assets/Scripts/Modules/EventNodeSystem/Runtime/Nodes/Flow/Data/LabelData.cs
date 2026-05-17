using System;
using Modules.EventNodeSystem.DataDefine;

namespace Modules.EventNodeSystem.Runtime.Nodes.Flow.Data
{
    [Serializable]
    public class LabelData : BaseNodeData
    {
        /// <summary>
        ///     标签名称。
        /// </summary>
        public string labelName;

        public override string GetSummary()
        {
            return $"▶ [标签] {labelName}";
        }
    }
}