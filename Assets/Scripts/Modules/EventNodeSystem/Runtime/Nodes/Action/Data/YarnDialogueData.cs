using System;
using System.Collections.Generic;
using Modules.EventNodeSystem.DataDefine;

namespace Modules.EventNodeSystem.Runtime.Nodes.Action.Data
{
    [Serializable]
    public class YarnDialogueData : BaseNodeData
    {
        /// <summary>
        ///     Yarn 文件中的节点名称。
        /// </summary>
        public string startNode = "Start";

        /// <summary>
        ///     根据 Yarn 路由信号走向的分支列表。
        /// </summary>
        public List<YarnBranchData> branches = new();

        /// <summary>
        ///     默认分支节点模板。
        /// </summary>
        public EventNode defaultNext;

        /// <summary>
        ///     默认分支节点数据。
        /// </summary>
        public BaseNodeData DefaultNextData;

        public override string GetSummary()
        {
            return $"◆ Yarn 对话: {startNode}";
        }

        [Serializable]
        public class YarnBranchData
        {
            /// <summary>
            ///     Yarn 路由名。
            /// </summary>
            public string routeName;

            /// <summary>
            ///     匹配后执行的节点模板。
            /// </summary>
            public EventNode nextNode;

            /// <summary>
            ///     匹配后执行的节点数据。
            /// </summary>
            public BaseNodeData NextData;
        }
    }
}