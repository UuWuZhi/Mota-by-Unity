using System;
using Modules.EventNodeSystem.DataDefine;
using Modules.EventNodeSystem.DataDefine.Context;
using Modules.EventNodeSystem.Runtime.Nodes.Flow.Data;
using UnityEngine;

namespace Modules.EventNodeSystem.Runtime.Nodes.Flow
{
    /// <summary>
    ///     已弃用的 For 节点：运行时不再参与执行，保留仅用于旧资产兼容与日志提示。
    /// </summary>
    [Obsolete("For 节点形式已弃用，请使用 Jump + 单条件节点 + 预制模板展开替代。")]
    [CreateAssetMenu(fileName = "For", menuName = "EventNodes/Flow/For")]
    public class ForNode : EventNode
    {
        /// <summary>
        ///     弃用节点的执行入口：不再执行真实逻辑，仅输出兼容日志并跳过。
        /// </summary>
        public override void Execute(BaseNodeData data, EventNodeContext ctx, System.Action onComplete)
        {
            Debug.LogWarning("ForNode: 节点形式已弃用，已跳过执行。请使用 Jump + 单条件节点的平铺结构。");
            onComplete?.Invoke();
        }
    }
}
