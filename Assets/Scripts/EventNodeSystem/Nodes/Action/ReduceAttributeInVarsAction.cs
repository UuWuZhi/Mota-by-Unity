using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ReduceAttributeInVarsAction", menuName = "EventNodes/Action/ReduceAttributeInVars")]
public class ReduceAttributeInVarsAction : ActionNode
{
    public AttributeType attributeType;
    public string valueVarName;
    public override void Execute(EventNodeContext ctx, Action onComplete)
    {
        ctx.Vars.TryGetValue(valueVarName, out object valueObj);
        if (ctx.PlayerAttribute != null)
        {
            int value = Convert.ToInt32(valueObj);
            ctx.PlayerAttribute.ReduceAttribute(attributeType, value);
        }
        onComplete?.Invoke();
    }
}