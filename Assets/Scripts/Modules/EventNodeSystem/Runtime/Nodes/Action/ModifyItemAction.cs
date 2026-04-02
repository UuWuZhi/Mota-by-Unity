using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ModifyItemAction", menuName = "EventNodes/Action/ModifyItem")]
public class ModifyItemAction : ActionNode
{
    public ModifyOperation operation = ModifyOperation.Add;
    public ModifyParameterSource parameterSource = ModifyParameterSource.Fixed;

    public ItemType itemType;
    public int count = 1;

    public string itemTypeVarKey = "itemType";
    public string countVarKey = "count";

    public override Type[] GetRequiredServices()
    {
        return new[] { typeof(IInventoryService) };
    }

    public override void Execute(EventNodeContext ctx, Action onComplete)
    {
        var inventoryService = ctx.GetService<IInventoryService>();
        if (inventoryService == null)
        {
            Debug.LogError("ModifyItemAction: InventoryService 未配置，无法执行。");
            onComplete?.Invoke();
            return;
        }

        if (!TryResolveParameters(ctx, out var resolvedEntries))
        {
            onComplete?.Invoke();
            return;
        }

        try
        {
            foreach (var entry in resolvedEntries)
            {
                ApplyOperation(inventoryService, entry.type, entry.count);
            }
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
        int current = inventoryService.GetItemCount(resolvedItemType);
        int delta = resolvedCount - current;
        if (delta > 0)
        {
            inventoryService.AddItem(resolvedItemType, delta);
        }
        else if (delta < 0)
        {
            inventoryService.RemoveItem(resolvedItemType, Mathf.Abs(delta));
        }
    }

    private void ApplyOperation(IInventoryService inventoryService, ItemType resolvedItemType, int resolvedCount)
    {
        if (resolvedCount <= 0)
        {
            Debug.LogWarning("ModifyItemAction: count <= 0，跳过执行。");
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
                Debug.LogWarning("ModifyItemAction: 未识别的操作类型。");
                break;
        }
    }

    private bool TryResolveParameters(EventNodeContext ctx, out System.Collections.Generic.List<(ItemType type, int count)> resolvedEntries)
    {
        resolvedEntries = new System.Collections.Generic.List<(ItemType type, int count)>();

        switch (parameterSource)
        {
            case ModifyParameterSource.Fixed:
                resolvedEntries.Add((itemType, count));
                return true;
            case ModifyParameterSource.TileUnit:
                return TryResolveFromTileUnit(ctx, resolvedEntries);
            case ModifyParameterSource.Vars:
                return TryResolveFromVars(ctx, resolvedEntries);
            default:
                Debug.LogWarning("ModifyItemAction: 未识别的参数来源。");
                return false;
        }
    }

    private bool TryResolveFromTileUnit(EventNodeContext ctx, System.Collections.Generic.List<(ItemType type, int count)> resolvedEntries)
    {
        if (ctx is not EventNodeTileContext tileCtx || tileCtx.TileObject == null)
        {
            Debug.LogWarning("ModifyItemAction: TileUnit 来源需要 EventNodeTileContext 与 TileObject。");
            return false;
        }

        var unit = tileCtx.TileObject.GetComponent<ItemUnit>();
        if (unit == null || unit.itemBonuses == null || unit.itemBonuses.Count == 0)
        {
            Debug.LogWarning("ModifyItemAction: 未找到 ItemUnit 或数据为空。");
            return false;
        }

        foreach (var bonus in unit.itemBonuses)
        {
            if (bonus == null) continue;
            resolvedEntries.Add((bonus.type, bonus.value));
        }

        return resolvedEntries.Count > 0;
    }

    private bool TryResolveFromVars(EventNodeContext ctx, System.Collections.Generic.List<(ItemType type, int count)> resolvedEntries)
    {
        if (ctx == null || ctx.Vars == null)
        {
            Debug.LogWarning("ModifyItemAction: Vars 来源需要有效的上下文。");
            return false;
        }

        if (!ctx.Vars.TryGetValue(itemTypeVarKey, out var itemObj))
        {
            Debug.LogWarning("ModifyItemAction: Vars 中未找到 itemType。");
            return false;
        }

        if (!ctx.Vars.TryGetValue(countVarKey, out var countObj))
        {
            Debug.LogWarning("ModifyItemAction: Vars 中未找到 count。");
            return false;
        }

        if (!TryParseItemType(itemObj, out var resolvedItemType))
        {
            Debug.LogWarning("ModifyItemAction: Vars 中的 itemType 无法解析。");
            return false;
        }

        if (!TryParseInt(countObj, out var resolvedCount))
        {
            Debug.LogWarning("ModifyItemAction: Vars 中的 count 无法解析。");
            return false;
        }

        resolvedEntries.Add((resolvedItemType, resolvedCount));
        return true;
    }

    private bool TryParseItemType(object value, out ItemType parsed)
    {
        parsed = itemType;
        if (value is ItemType item)
        {
            parsed = item;
            return true;
        }

        if (value is int intValue)
        {
            parsed = (ItemType)intValue;
            return true;
        }

        if (value is string strValue && Enum.TryParse(strValue, true, out ItemType enumValue))
        {
            parsed = enumValue;
            return true;
        }

        return false;
    }

    private bool TryParseInt(object value, out int parsed)
    {
        parsed = 0;
        if (value is int intValue)
        {
            parsed = intValue;
            return true;
        }

        if (value is float floatValue)
        {
            parsed = Mathf.RoundToInt(floatValue);
            return true;
        }

        if (value is string strValue && int.TryParse(strValue, out int result))
        {
            parsed = result;
            return true;
        }

        return false;
    }
}
