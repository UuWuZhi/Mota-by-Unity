using System;
using UnityEngine;

[CreateAssetMenu(fileName = "AddAttributeAction", menuName = "EventNodes/Action/AddAttribute")]
public class AddAttributeAction : ActionNode
{
    public AttributeType attributeType;
    public int value = 1;

    public override void Execute(EventNodeContext ctx, Action onComplete)
    {
        // 简单调用背包接口
        if (ctx.PlayerAttribute != null)
        {
            ctx.PlayerAttribute.AddAttribute(attributeType, value);
        }
        onComplete?.Invoke();
    }
}