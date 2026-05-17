using System;

namespace Modules.EventNodeSystem.Runtime.Nodes.Condition.Data
{
    /// <summary>
    ///     比较模式枚举，用于条件节点的比较配置。
    /// </summary>
    public enum ComparisonMode
    {
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual,
        Equal,
        NotEqual
    }
}
