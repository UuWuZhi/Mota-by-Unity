using System;
using Modules.EventNodeSystem.DataDefine;
using Modules.EventNodeSystem.DataDefine.Context;

namespace Modules.EventNodeSystem.Runtime.Nodes.Action.Data
{
    [Serializable]
    public class SetVariableData : BaseNodeData
    {
        public enum Operation
        {
            Set,
            Increment,
            Decrement,
            Toggle
        }

        public enum VarType
        {
            Bool,
            Int,
            Float,
            String
        }

        /// <summary>
        ///     目标上下文变量键。
        /// </summary>
        public ContextVarKey key = ContextVarKey.AllowEnter;

        /// <summary>
        ///     变量类型。
        /// </summary>
        public VarType varType = VarType.Bool;

        /// <summary>
        ///     操作类型。
        /// </summary>
        public Operation operation = Operation.Set;

        /// <summary>
        ///     Bool 类型值。
        /// </summary>
        public bool boolValue;

        /// <summary>
        ///     Int 类型值。
        /// </summary>
        public int intValue;

        /// <summary>
        ///     Float 类型值。
        /// </summary>
        public float floatValue;

        /// <summary>
        ///     String 类型值。
        /// </summary>
        public string stringValue;

        public override string GetSummary()
        {
            return $"◆ 设置变量: {key} ({varType})";
        }
    }
}