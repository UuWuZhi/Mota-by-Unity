using System;
using UnityEngine;

[CreateAssetMenu(fileName = "AddAttributeAction", menuName = "EventNodes/Action/AddAttribute")]
public class AddAttributeAction : ActionNode
{
    public override Type[] GetRequiredServices()
    {
        return new[] { typeof(PlayerAttribute) };
    }

    public AttributeType attributeType;
    public int value = 1;

    public override void Execute(EventNodeContext ctx, Action onComplete)
    {
        var playerAttribute = ctx.GetService<PlayerAttribute>();
        if (playerAttribute != null)
        {
            playerAttribute.AddAttribute(attributeType, value);
        }
        onComplete?.Invoke();
    }
}