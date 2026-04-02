using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ReadAttributeInUnitAction", menuName = "EventNodes/Action/ReadAttributeInUnit")]
public class ReadAttributeInUnitAction : TileActionNode
{
    // 写入到 ctx.Vars 的临时数据
    public ContextVarKey valueMapKey = ContextVarKey.AttributeValueMap;
    public ContextVarKey bonusListKey = ContextVarKey.AttributeBonusList;

    public override void ExecuteTile(EventNodeTileContext ctx, Action onComplete)
    {
        if (ctx == null)
        {
            onComplete?.Invoke();
            return;
        }

        var attributeUnit = ctx.TileObject != null ? ctx.TileObject.GetComponent<AttributeUnit>() : null;
        if (attributeUnit == null)
        {
            Debug.LogError("ReadAttributeInUnitAction: 目标没有 AttributeUnit 组件");
            onComplete?.Invoke();
            return;
        }

        var list = attributeUnit.attributeBonuses;
        if (list == null || list.Count == 0)
        {
            // 清理可能存在的旧变量（可选）
            ctx.Set(bonusListKey, new List<AttributeBonus>());
            ctx.Set(valueMapKey, new Dictionary<AttributeType, int>());
            onComplete?.Invoke();
            return;
        }

        // 聚合相同属性类型的 value
        var aggregated = new Dictionary<AttributeType, int>();
        foreach (var ab in list)
        {
            if (ab == null) continue;
            if (!aggregated.ContainsKey(ab.Type)) aggregated[ab.Type] = 0;
            aggregated[ab.Type] += ab.Value;
        }

        ctx.Set(valueMapKey, aggregated);
        ctx.Set(bonusListKey, new List<AttributeBonus>(list));

        Debug.Log($"ReadAttributeInUnitAction: 写入 {aggregated.Count} 个属性到 ctx.Vars");

        onComplete?.Invoke();
    }
}