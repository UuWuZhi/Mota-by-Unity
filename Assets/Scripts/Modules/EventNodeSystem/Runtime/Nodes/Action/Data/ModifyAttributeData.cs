using System;
using Modules.Core.DataDefine;
using Modules.EventNodeSystem.DataDefine;
using Modules.EventNodeSystem.DataDefine.Context;
using Modules.EventNodeSystem.DataDefine.Data;

namespace Modules.EventNodeSystem.Runtime.Nodes.Action.Data
{
    [Serializable]
    public class ModifyAttributeData : BaseNodeData
    {
        /// <summary>
        ///     属性变更操作类型。
        /// </summary>
        public ModifyOperation operation = ModifyOperation.Add;

        /// <summary>
        ///     参数来源方式。
        /// </summary>
        public ModifyParameterSource parameterSource = ModifyParameterSource.Fixed;

        /// <summary>
        ///     目标属性类型。
        /// </summary>
        public AttributeType attributeType;

        /// <summary>
        ///     变更数值。
        /// </summary>
        public int value = 1;

        /// <summary>
        ///     从上下文变量读取数值时使用的键。
        /// </summary>
        public ContextVarKey valueVarKey = ContextVarKey.PlayerHpLoss;

        public override string GetSummary()
        {
            return $"◆ 修改属性: {attributeType} {operation} {value}";
        }
    }
}