using System;
using Modules.EventNodeSystem.DataDefine;
using Modules.Item.DataDefine;

namespace Modules.EventNodeSystem.Runtime.Nodes.Condition.Data
{
    /// <summary>
    ///     玩家持有物品条件数据。
    /// </summary>
    [Serializable]
    public class PlayerHasItemData : BaseNodeData
    {
        /// <summary>
        ///     物品类型。
        /// </summary>
        public ItemType itemType;

        /// <summary>
        ///     需要的物品数量。
        /// </summary>
        public int requiredCount = 1;

        /// <summary>
        ///     比较模式（默认为 GreaterOrEqual）。
        /// </summary>
        public ComparisonMode comparisonMode = ComparisonMode.GreaterOrEqual;

        /// <summary>
        ///     获取条件摘要。
        /// </summary>
        public override string GetSummary()
        {
            var op = comparisonMode switch
            {
                ComparisonMode.Greater => ">",
                ComparisonMode.GreaterOrEqual => ">=",
                ComparisonMode.Less => "<",
                ComparisonMode.LessOrEqual => "<=",
                ComparisonMode.Equal => "==",
                ComparisonMode.NotEqual => "!=",
                _ => ">="
            };
            return $"◆ 条件：持有物品 {itemType} {op} {requiredCount}";
        }
    }
}