using System;
using Modules.EventNodeSystem.DataDefine;
using Modules.Item.DataDefine;

namespace Modules.EventNodeSystem.Runtime.Nodes.Action.Data
{
    [Serializable]
    public class PickaxeActionData : BaseNodeData
    {
        /// <summary>
        ///     需要的工具类型。
        /// </summary>
        public ItemType requiredTool = ItemType.Pickaxe;

        public override string GetSummary()
        {
            return $"◆ 使用工具: {requiredTool}";
        }
    }
}