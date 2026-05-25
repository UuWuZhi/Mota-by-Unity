using System;
using Modules.Core.Runtime;
using Modules.EventNodeSystem.DataDefine;
using Modules.EventNodeSystem.DataDefine.Context;
using Modules.EventNodeSystem.Runtime.Nodes.Action.Data;
using Modules.Map.Runtime;
using UnityEngine;

namespace Modules.EventNodeSystem.Runtime.Nodes.Action.TileAction
{
    [CreateAssetMenu(fileName = "RemoveTile", menuName = "EventNodes/Action/RemoveTile")]
    public class RemoveTile : TileActionNode
    {
        public override Type[] GetRequiredServices()
        {
            return new[] { typeof(GridManager) };
        }

        public override void ExecuteTile(BaseNodeData data, EventNodeTileContext ctx, System.Action onComplete)
        {
            if (data is not RemoveTileData)
            {
                DebugEditor.LogWarning("RemoveTile: data 类型不匹配，跳过执行。");
                onComplete?.Invoke();
                return;
            }

            var gridManager = ctx?.GetService<GridManager>();
            if (gridManager == null)
            {
                DebugEditor.LogWarning("RemoveTile: GridManager 未初始化");
                onComplete?.Invoke();
                return;
            }

            var cell = ctx.CellPos;

            gridManager.RemoveEventTileAtCell(cell);

            onComplete?.Invoke();
        }
    }
}