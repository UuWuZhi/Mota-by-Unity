using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ReadAttributeInUnitAction", menuName = "EventNodes/Action/ReadAttributeInUnit")]
public class ReadAttributeInUnitAction : ActionNode
{
    // 写入到 ctx.Vars 的 key 前缀，可在 Inspector 中调整
    public string variablePrefix = "attribute";

    public override void Execute(EventNodeContext ctx, Action onComplete)
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
            ctx.Vars[$"{variablePrefix}list"] = new List<AttributeBonus>();
            onComplete?.Invoke();
            return;
        }

        // 聚合相同属性类型的 value
        var aggregated = new Dictionary<AttributeType, int>();
        foreach (var ab in list)
        {
            if (ab == null) continue;
            if (!aggregated.ContainsKey(ab.type)) aggregated[ab.type] = 0;
            aggregated[ab.type] += ab.value;
        }

        // 写入单项属性到 ctx.Vars，键名示例： "attribute_HP"
        foreach (var kv in aggregated)
        {
            string key = $"{variablePrefix}{kv.Key}";
            ctx.Vars[key] = kv.Value;
        }

        Debug.Log($"ReadAttributeInUnitAction: 写入 {aggregated.Count} 个属性到 ctx.Vars");

        onComplete?.Invoke();
    }
}