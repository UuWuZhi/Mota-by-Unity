using System;
using System.Collections.Generic;
using Modules.Core.DataDefine.Units;
using Modules.EventNodeSystem.DataDefine;
using Modules.EventNodeSystem.DataDefine.Context;
using Modules.EventNodeSystem.DataDefine.Data;
using Modules.EventNodeSystem.Runtime.Nodes.Action.Data;
using Modules.Item.DataDefine;
using Modules.Player.DataDefine;
using UnityEngine;

namespace Modules.EventNodeSystem.Runtime.Nodes.Action
{
    [CreateAssetMenu(fileName = "ModifyItem", menuName = "EventNodes/Action/ModifyItem")]
    public class ModifyItem : ActionNode
    {
        public override Type[] GetRequiredServices()
        {
            return new[] { typeof(IInventoryService) };
        }

        public override void Execute(BaseNodeData data, EventNodeContext ctx, System.Action onComplete)
        {
            var modifyData = data as ModifyItemData;
            if (modifyData == null)
            {
                Debug.LogWarning("ModifyItem: data 类型不匹配，跳过执行。");
                onComplete?.Invoke();
                return;
            }

            var inventoryService = ctx.GetService<IInventoryService>();
            if (inventoryService == null)
            {
                Debug.LogError("ModifyItem: InventoryService 未配置，无法执行。");
                onComplete?.Invoke();
                return;
            }

            if (!TryResolveParameters(modifyData, ctx, out var resolvedEntries))
            {
                onComplete?.Invoke();
                return;
            }

            try
            {
                foreach (var entry in resolvedEntries)
                    ApplyOperation(modifyData.operation, inventoryService, entry.type, entry.count);
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

        private void ApplySetOperation(IInventoryService inventoryService, ItemType resolvedItemType, int resolvedCount)
        {
            var current = inventoryService.GetItemCount(resolvedItemType);
            var delta = resolvedCount - current;
            if (delta > 0)
                inventoryService.AddItem(resolvedItemType, delta);
            else if (delta < 0) inventoryService.RemoveItem(resolvedItemType, Mathf.Abs(delta));
        }

        private void ApplyOperation(ModifyOperation operation, IInventoryService inventoryService,
            ItemType resolvedItemType, int resolvedCount)
        {
            if (resolvedCount <= 0)
            {
                Debug.LogWarning("ModifyItem: count <= 0，跳过执行。");
                return;
            }

            switch (operation)
            {
                case ModifyOperation.Add:
                    inventoryService.AddItem(resolvedItemType, resolvedCount);
                    break;
                case ModifyOperation.Remove:
                    inventoryService.RemoveItem(resolvedItemType, resolvedCount);
                    break;
                case ModifyOperation.Set:
                    ApplySetOperation(inventoryService, resolvedItemType, resolvedCount);
                    break;
                default:
                    Debug.LogWarning("ModifyItem: 未识别的操作类型。");
                    break;
            }
        }

        private bool TryResolveParameters(ModifyItemData data, EventNodeContext ctx,
            out List<(ItemType type, int count)> resolvedEntries)
        {
            resolvedEntries = new List<(ItemType type, int count)>();

            switch (data.parameterSource)
            {
                case ModifyParameterSource.Fixed:
                    resolvedEntries.Add((data.itemType, data.count));
                    return true;
                case ModifyParameterSource.TileUnit:
                    return TryResolveFromTileUnit(ctx, resolvedEntries);
                case ModifyParameterSource.Vars:
                    return TryResolveFromVars(data, ctx, resolvedEntries);
                default:
                    Debug.LogWarning("ModifyItem: 未识别的参数来源。");
                    return false;
            }
        }

        private bool TryResolveFromTileUnit(EventNodeContext ctx, List<(ItemType type, int count)> resolvedEntries)
        {
            if (ctx is not EventNodeTileContext tileCtx || tileCtx.TileObject == null)
            {
                Debug.LogWarning("ModifyItem: TileUnit 来源需要 EventNodeTileContext 与 TileObject。");
                return false;
            }

            var unit = tileCtx.TileObject.GetComponent<ItemUnit>();
            if (unit == null || unit.itemBonuses == null || unit.itemBonuses.Count == 0)
            {
                Debug.LogWarning("ModifyItem: 未找到 ItemUnit 或数据为空。");
                return false;
            }

            foreach (var bonus in unit.itemBonuses)
            {
                if (bonus == null) continue;
                resolvedEntries.Add((bonus.type, bonus.value));
            }

            return resolvedEntries.Count > 0;
        }

        private bool TryResolveFromVars(ModifyItemData data, EventNodeContext ctx,
            List<(ItemType type, int count)> resolvedEntries)
        {
            if (ctx == null || ctx.Vars == null)
            {
                Debug.LogWarning("ModifyItem: Vars 来源需要有效的上下文。");
                return false;
            }

            if (!ctx.TryGet(data.valueVarKey, out int resolvedCount))
            {
                Debug.LogWarning($"ModifyItem: Vars 中未找到 {data.valueVarKey}。");
                return false;
            }

            resolvedEntries.Add((data.itemType, resolvedCount));
            return true;
        }
    }
}