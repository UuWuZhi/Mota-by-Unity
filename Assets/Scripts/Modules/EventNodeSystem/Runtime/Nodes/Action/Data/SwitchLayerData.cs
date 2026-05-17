using System;
using Modules.EventNodeSystem.DataDefine;
using Modules.Map.DataDefine;

namespace Modules.EventNodeSystem.Runtime.Nodes.Action.Data
{
    [Serializable]
    public class SwitchLayerData : BaseNodeData
    {
        /// <summary>
        ///     楼梯类型。
        /// </summary>
        public StairType stairType;

        public override string GetSummary()
        {
            return $"◆ 切换楼层: {stairType}";
        }
    }
}