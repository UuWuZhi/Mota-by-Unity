using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AddAttributeInVarsAction", menuName = "EventNodes/Action/AddAttributeInVars")]
public class AddAttributeInVarsAction : ActionNode
{
    public AttributeType attributeType;
    public ContextVarKey valueVarKey = ContextVarKey.AttributeValue;

    public override Type[] GetRequiredServices()
    {
        return new[] { typeof(PlayerAttribute) };
    }

    public override void Execute(EventNodeContext ctx, Action onComplete)
    {
        if (ctx == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (!ctx.TryGet(valueVarKey, out object valueObj))
        {
            onComplete?.Invoke();
            return;
        }
        var playerAttribute = ctx.GetService<PlayerAttribute>();
        if (playerAttribute != null)
        {
            int value = ResolveValue(valueObj);
            playerAttribute.AddAttribute(attributeType, value);
        }
        onComplete?.Invoke();
    }

    private int ResolveValue(object valueObj)
    {
        if (valueObj is Dictionary<AttributeType, int> valueMap && valueMap.TryGetValue(attributeType, out var mapped))
        {
            return mapped;
        }

        return Convert.ToInt32(valueObj);
    }
}