using System;
using Modules.EventNodeSystem.DataDefine;
using Modules.EventNodeSystem.DataDefine.Context;
using Modules.EventNodeSystem.Runtime.Nodes.Action.Data;
using Modules.Map.Runtime;
using Modules.Player.DataDefine;
using Modules.Player.Runtime;
using UnityEngine;

namespace Modules.EventNodeSystem.Runtime.Nodes.Action.ItemAction
{
    [CreateAssetMenu(fileName = "PickaxeAction", menuName = "EventNodes/Action/Item/Pickaxe")]
    public class PickaxeActionNode : ItemActionNode
    {
        public override Type[] GetRequiredServices()
        {
            // 运行时我们希望注入 GridManager 与 IInventoryService；PlayerState 可能通过 caller 获取
            return new[] { typeof(GridManager), typeof(IInventoryService) };
        }

        public override void ExecuteItem(BaseNodeData data, ItemEventContext ctx, System.Action onComplete)
        {
            var pickaxeData = data as PickaxeActionData;
            if (pickaxeData == null)
            {
                Debug.LogWarning("PickaxeActionNode: data 类型不匹配，跳过执行。");
                onComplete?.Invoke();
                return;
            }

            try
            {
                Debug.Log("PickaxeActionNode: 执行挖掘逻辑");
                // 获取 GridManager
                var grid = ctx.GetService<GridManager>();

                // 获取 Inventory service
                var inventory = ctx.GetService<IInventoryService>();

                // 尝试获取 PlayerState：优先从 ctx 服务取，其次尝试从 vars 中的 caller 获取组件
                PlayerState player = null;
                if (ctx.TryGetService<PlayerState>(out var ps)) player = ps;
                else if (ctx.TryGet(ContextVarKey.Caller, out GameObject go)) player = go.GetComponent<PlayerState>();

                if (grid == null || player == null)
                {
                    Debug.LogWarning("PickaxeActionNode: 缺少 GridManager 或 PlayerState，跳过执行");
                    onComplete?.Invoke();
                    return;
                }

                // Use the player's current world position to compute the cell to avoid stale PlayerState.CellPos
                var playerWorldPos = player.transform.position;
                if (!grid.TryWorldToCellPos(playerWorldPos, out var playerCell))
                {
                    Debug.LogWarning("[PickaxeActionNode]:玩家世界坐标转换为格子坐标失败");
                    return;
                }
                var targetCell = playerCell;
                switch (player.Facing)
                {
                    case Facing.Up: targetCell += new Vector3Int(0, 1, 0); break;
                    case Facing.Down: targetCell += new Vector3Int(0, -1, 0); break;
                    case Facing.Left: targetCell += new Vector3Int(-1, 0, 0); break;
                    case Facing.Right: targetCell += new Vector3Int(1, 0, 0); break;
                }

                if (!grid.IsInGridBounds(targetCell))
                {
                    Debug.LogWarning($"PickaxeActionNode: 目标格子 {targetCell} 超出地图边界");
                    onComplete?.Invoke();
                    return;
                }

                var obstacle = grid.GetObstacleTileAtCell(targetCell);
                if (obstacle == null || !obstacle.isBreakable)
                {
                    Debug.LogWarning($"PickaxeActionNode: 目标格子 {targetCell} 没有可挖掘的障碍物");
                    onComplete?.Invoke();
                    return;
                }

                // 检查是否需要特定工具
                if (obstacle.breakableBy != null && obstacle.breakableBy.Count > 0)
                {
                    var hasRequired = false;
                    foreach (var it in obstacle.breakableBy)
                        if (it == pickaxeData.requiredTool && inventory != null && inventory.HasItem(it))
                        {
                            hasRequired = true;
                            break;
                        }

                    if (!hasRequired)
                    {
                        Debug.LogWarning(
                            $"PickaxeActionNode: 目标格子 {targetCell} 的障碍物需要特定工具 {pickaxeData.requiredTool}，但玩家没有");
                        onComplete?.Invoke();
                        return;
                    }
                }

                var removed = grid.RemoveObstacleTileAtCell(targetCell);
                if (!removed)
                {
                    Debug.LogWarning($"PickaxeActionNode: 无法移除目标格子 {targetCell} 的障碍物");
                    onComplete?.Invoke();
                    return;
                }

                // 标记成功：使用 ItemEventContext 的约定
                ctx.MarkUseSucceeded();
                Debug.Log($"PickaxeActionNode: 成功挖掘格子 {targetCell} 的障碍物");

                // 如果希望节点直接消费物品，可以在此调用 inventory.RemoveItem
                // 但推荐由调用方（ItemUseHandler）根据 useMode 统一消费，或在节点中通过约定直接消费
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                onComplete?.Invoke();
            }
        }
    }
}