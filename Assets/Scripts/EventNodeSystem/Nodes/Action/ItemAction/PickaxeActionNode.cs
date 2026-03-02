using System;
using UnityEngine;

[CreateAssetMenu(fileName = "PickaxeAction", menuName = "EventNodes/Action/Item/Pickaxe")]
public class PickaxeActionNode : ActionNode
{
    // 可配置是否需要特定工具或伤害等（留作扩展）
    public ItemType requiredTool = ItemType.Pickaxe;

    public override Type[] GetRequiredServices()
    {
        // 运行时我们希望注入 GridManager 与 IInventoryService；PlayerState 可能通过 caller 获取
        return new[] { typeof(GridManager), typeof(IInventoryService) };
    }

    public override void Execute(EventNodeContext ctx, Action onComplete)
    {
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
            else if (ctx.Vars != null && ctx.Vars.TryGetValue("caller", out var callerObj) && callerObj is GameObject go)
            {
                player = go.GetComponent<PlayerState>();
            }

            if (grid == null || player == null)
            {
                Debug.LogWarning("PickaxeActionNode: 缺少 GridManager 或 PlayerState，跳过执行");
                // 标记失败
                if (ctx != null && ctx.Vars != null) ctx.Vars["use_succeeded"] = false;
                onComplete?.Invoke();
                return;
            }

            // Use the player's current world position to compute the cell to avoid stale PlayerState.CellPos
            Vector3 playerWorldPos = player.transform.position;
            Vector3Int playerCell = grid.MapGrid.WorldToCell(playerWorldPos);
            Vector3Int targetCell = playerCell;
            switch (player.Facing)
            {
                case Facing.Up: targetCell += new Vector3Int(0, 1, 0); break;
                case Facing.Down: targetCell += new Vector3Int(0, -1, 0); break;
                case Facing.Left: targetCell += new Vector3Int(-1, 0, 0); break;
                case Facing.Right: targetCell += new Vector3Int(1, 0, 0); break;
            }

            if (!grid.IsInGridBounds(targetCell))
            {
                ctx.Vars["use_succeeded"] = false;
                Debug.LogWarning($"PickaxeActionNode: 目标格子 {targetCell} 超出地图边界");
                onComplete?.Invoke();
                return;
            }

            var obstacle = grid.GetObstacleTileAtCell(targetCell);
            if (obstacle == null || !obstacle.IsBreakable)
            {
                ctx.Vars["use_succeeded"] = false;
                Debug.LogWarning($"PickaxeActionNode: 目标格子 {targetCell} 没有可挖掘的障碍物");
                onComplete?.Invoke();
                return;
            }

            // 检查是否需要特定工具
            if (obstacle.BreakableBy != null && obstacle.BreakableBy.Count > 0)
            {
                bool hasRequired = false;
                foreach (var it in obstacle.BreakableBy)
                {
                    if (it == requiredTool && inventory != null && inventory.HasItem(it)) { hasRequired = true; break; }
                }
                if (!hasRequired)
                {
                    Debug.LogWarning($"PickaxeActionNode: 目标格子 {targetCell} 的障碍物需要特定工具 {requiredTool}，但玩家没有");
                    ctx.Vars["use_succeeded"] = false;
                    onComplete?.Invoke();
                    return;
                }
            }

            bool removed = grid.RemoveObstacleTileAtCell(targetCell);
            if (!removed)
            {
                Debug.LogWarning($"PickaxeActionNode: 无法移除目标格子 {targetCell} 的障碍物");
                ctx.Vars["use_succeeded"] = false;
                onComplete?.Invoke();
                return;
            }

            // 标记成功：优先使用 ItemEventContext 的约定
            if (ctx is ItemEventContext iec2) iec2.MarkUseSucceeded();
            else ctx.Vars["use_succeeded"] = true;
            Debug.Log($"PickaxeActionNode: 成功挖掘格子 {targetCell} 的障碍物");

            // 如果希望节点直接消费物品，可以在此调用 inventory.RemoveItem
            // 但推荐由调用方（ItemUseHandler）根据 useMode 统一消费，或在节点中通过约定直接消费

        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            if (ctx != null && ctx.Vars != null) ctx.Vars["use_succeeded"] = false;
        }
        finally
        {
            onComplete?.Invoke();
        }
    }
}
