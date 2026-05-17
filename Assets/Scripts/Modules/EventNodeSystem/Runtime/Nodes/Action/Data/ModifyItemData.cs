using System;
using Modules.EventNodeSystem.DataDefine;
using Modules.EventNodeSystem.DataDefine.Context;
using Modules.EventNodeSystem.DataDefine.Data;
using Modules.Item.DataDefine;

namespace Modules.EventNodeSystem.Runtime.Nodes.Action.Data
{
    [Serializable]
    public class ModifyItemData : BaseNodeData
    {
        /// <summary>
        ///     物品变更操作类型。
        /// </summary>
        public ModifyOperation operation = ModifyOperation.Add;

        /// <summary>
        ///     参数来源方式。
        /// </summary>
        public ModifyParameterSource parameterSource = ModifyParameterSource.Fixed;

        /// <summary>
        ///     目标物品类型。
        /// </summary>
        public ItemType itemType;

        /// <summary>
        ///     变更数量。
        /// </summary>
        public int count = 1;

        /// <summary>
        ///     从上下文变量读取数量时使用的键。
        /// </summary>
        public ContextVarKey valueVarKey = ContextVarKey.UseCount;

        public override string GetSummary()
        {
            return $"◆ 修改物品: {itemType} {operation} {count}";
        }
    }
}