using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ModifyAttributeAction", menuName = "EventNodes/Action/ModifyAttribute")]
public class ModifyAttributeAction : ActionNode
{
    public ModifyOperation operation = ModifyOperation.Add;
    public ModifyParameterSource parameterSource = ModifyParameterSource.Fixed;

    public AttributeType attributeType;
    public int value = 1;

    public string attributeTypeVarKey = "attributeType";
    public string valueVarKey = "value";

    public override Type[] GetRequiredServices()
    {
        return new[] { typeof(PlayerAttribute) };
    }

    public override void Execute(EventNodeContext ctx, Action onComplete)
    {
        var playerAttribute = ctx.GetService<PlayerAttribute>();
        if (playerAttribute == null)
        {
            Debug.LogError("ModifyAttributeAction: PlayerAttribute 未配置，无法执行。");
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
                ApplyOperation(playerAttribute, entry.type, entry.value);
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

    private void ApplyOperation(PlayerAttribute playerAttribute, AttributeType resolvedType, int resolvedValue)
    {
        if (resolvedValue <= 0)
        {
            Debug.LogWarning("ModifyAttributeAction: value <= 0，跳过执行。");
            return;
        }

        switch (operation)
        {
            case ModifyOperation.Add:
                playerAttribute.AddAttribute(resolvedType, resolvedValue);
                break;
            case ModifyOperation.Remove:
                playerAttribute.ReduceAttribute(resolvedType, resolvedValue);
                break;
            case ModifyOperation.Set:
                playerAttribute.SetAttributeValue(resolvedType, resolvedValue);
                break;
            default:
                Debug.LogWarning("ModifyAttributeAction: 未识别的操作类型。");
                break;
        }
    }

    private bool TryResolveParameters(EventNodeContext ctx, out List<(AttributeType type, int value)> resolvedEntries)
    {
        resolvedEntries = new List<(AttributeType type, int value)>();

        switch (parameterSource)
        {
            case ModifyParameterSource.Fixed:
                resolvedEntries.Add((attributeType, value));
                return true;
            case ModifyParameterSource.TileUnit:
                return TryResolveFromTileUnit(ctx, resolvedEntries);
            case ModifyParameterSource.Vars:
                return TryResolveFromVars(ctx, resolvedEntries);
            default:
                Debug.LogWarning("ModifyAttributeAction: 未识别的参数来源。");
                return false;
        }
    }

    private bool TryResolveFromTileUnit(EventNodeContext ctx, List<(AttributeType type, int value)> resolvedEntries)
    {
        if (ctx is not EventNodeTileContext tileCtx || tileCtx.TileObject == null)
        {
            Debug.LogWarning("ModifyAttributeAction: TileUnit 来源需要 EventNodeTileContext 与 TileObject。");
            return false;
        }

        var valueUnit = tileCtx.TileObject.GetComponent<AttributeValueUnit>();
        if (valueUnit != null)
        {
            resolvedEntries.Add((valueUnit.attributeType, valueUnit.value));
            return true;
        }

        var unit = tileCtx.TileObject.GetComponent<AttributeUnit>();
        if (unit == null || unit.attributeBonuses == null || unit.attributeBonuses.Count == 0)
        {
            Debug.LogWarning("ModifyAttributeAction: 未找到 AttributeUnit 或数据为空。");
            return false;
        }

        foreach (var bonus in unit.attributeBonuses)
        {
            if (bonus == null) continue;
            resolvedEntries.Add((bonus.Type, bonus.Value));
        }

        return resolvedEntries.Count > 0;
    }

    private bool TryResolveFromVars(EventNodeContext ctx, List<(AttributeType type, int value)> resolvedEntries)
    {
        if (ctx == null || ctx.Vars == null)
        {
            Debug.LogWarning("ModifyAttributeAction: Vars 来源需要有效的上下文。");
            return false;
        }

        if (!ctx.Vars.TryGetValue(attributeTypeVarKey, out var typeObj))
        {
            Debug.LogWarning("ModifyAttributeAction: Vars 中未找到 attributeType。");
            return false;
        }

        if (!ctx.Vars.TryGetValue(valueVarKey, out var valueObj))
        {
            Debug.LogWarning("ModifyAttributeAction: Vars 中未找到 value。");
            return false;
        }

        if (!TryParseAttributeType(typeObj, out var resolvedType))
        {
            Debug.LogWarning("ModifyAttributeAction: Vars 中的 attributeType 无法解析。");
            return false;
        }

        if (!TryParseInt(valueObj, out var resolvedValue))
        {
            Debug.LogWarning("ModifyAttributeAction: Vars 中的 value 无法解析。");
            return false;
        }

        resolvedEntries.Add((resolvedType, resolvedValue));
        return true;
    }

    private bool TryParseAttributeType(object value, out AttributeType parsed)
    {
        parsed = attributeType;
        if (value is AttributeType attr)
        {
            parsed = attr;
            return true;
        }

        if (value is int intValue)
        {
            parsed = (AttributeType)intValue;
            return true;
        }

        if (value is string strValue && Enum.TryParse(strValue, true, out AttributeType enumValue))
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
