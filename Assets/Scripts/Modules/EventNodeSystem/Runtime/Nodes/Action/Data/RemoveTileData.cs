using System;
using Modules.EventNodeSystem.DataDefine;

namespace Modules.EventNodeSystem.Runtime.Nodes.Action.Data
{
    [Serializable]
    public class RemoveTileData : BaseNodeData
    {
        public override string GetSummary()
        {
            return "◆ 移除事件格";
        }
    }
}