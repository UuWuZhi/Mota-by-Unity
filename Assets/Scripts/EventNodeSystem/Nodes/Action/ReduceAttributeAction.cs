using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ReduceAttributeAction", menuName = "EventNodes/Action/ReduceAttribute")]
public class ReduceAttributeAction : ActionNode
{
    public AttributeType attributeType;
    public int value = 1;

    public override Type[] GetRequiredServices()
    {
        return new[] { typeof(PlayerAttribute) };
    }

    public override void Execute(EventNodeContext ctx, Action onComplete)
    {
        var playerAttribute = ctx.GetService<PlayerAttribute>();
        if (playerAttribute != null)
        {
            playerAttribute.ReduceAttribute(attributeType, value);
        }
        onComplete?.Invoke();
    }
}