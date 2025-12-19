using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ReduceAttributeAction", menuName = "EventNodes/Action/ReduceAttribute")]
public class ReduceAttributeAction : ActionNode
{
    public AttributeType attributeType;
    public int value = 1;

    public override void Execute(EventNodeContext ctx, Action onComplete)
    {
        // 简单调用背包接口
        if (ctx.PlayerAttribute != null)
        {
            ctx.PlayerAttribute.ReduceAttribute(attributeType, value);
        }
        onComplete?.Invoke();
    }
}