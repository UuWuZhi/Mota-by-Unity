using System;
using UnityEngine;

[CreateAssetMenu(fileName = "AddAttributeInVarsAction", menuName = "EventNodes/Action/AddAttributeInVars")]
public class AddAttributeInVarsAction : ActionNode
{
    public AttributeType attributeType;
    public string valueVarName;
    public override void Execute(EventNodeContext ctx, Action onComplete)
    {
        ctx.Vars.TryGetValue(valueVarName, out object valueObj);
        // 简单调用背包接口
        if (ctx.PlayerAttribute != null)
        {
            int value = Convert.ToInt32(valueObj);
            ctx.PlayerAttribute.AddAttribute(attributeType, value);
        }
        onComplete?.Invoke();
    }
}