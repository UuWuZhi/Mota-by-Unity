using System;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerHasAttributeCondition", menuName = "EventNodes/Condition/PlayerHasAttribute")]
public class PlayerHasAttributeCondition : ConditionNode
{
    public AttributeType attributeType;
    public int requiredValue = 1;

    public override void Evaluate(EventNodeContext ctx, Action<bool> onResult)
    {
        //Debug.Log("Node:检测物品开始");
        bool hasAttribute;
        try
        {
            hasAttribute = ctx.PlayerAttribute != null && ctx.PlayerAttribute.HasAttributeValue(attributeType, requiredValue);
        }
        catch { hasAttribute = false; }
        //Debug.Log($"物品检测结果：{hasAttribute}");
        onResult?.Invoke(hasAttribute);
    }
}