using System;
using UnityEngine;

[CreateAssetMenu(fileName = "AddAttributeInVarsAction", menuName = "EventNodes/Action/AddAttributeInVars")]
public class AddAttributeInVarsAction : ActionNode
{
    public AttributeType attributeType;
    public string valueVarName;

    public override Type[] GetRequiredServices()
    {
        return new[] { typeof(PlayerAttribute) };
    }

    public override void Execute(EventNodeContext ctx, Action onComplete)
    {
        ctx.Vars.TryGetValue(valueVarName, out object valueObj);
        var playerAttribute = ctx.GetService<PlayerAttribute>();
        if (playerAttribute != null)
        {
            int value = Convert.ToInt32(valueObj);
            playerAttribute.AddAttribute(attributeType, value);
        }
        onComplete?.Invoke();
    }
}