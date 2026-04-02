using System;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerHasAttributeCondition", menuName = "EventNodes/Condition/PlayerHasAttribute")]
public class PlayerHasAttributeCondition : ConditionNode
{
    public AttributeType attributeType;
    public int requiredValue = 1;

    public override Type[] GetRequiredServices()
    {
        return new[] { typeof(PlayerAttribute) };
    }

    public override void Evaluate(EventNodeContext ctx, Action<bool> onResult)
    {
        //Debug.Log("Node:检测物品开始");
        bool hasAttribute;
        try
        {
            var playerAttribute = ctx?.GetService<PlayerAttribute>();
            hasAttribute = playerAttribute != null && playerAttribute.HasAttributeValue(attributeType, requiredValue);
            if (playerAttribute == null)
            {
                Debug.LogWarning("PlayerHasAttributeCondition: PlayerAttribute 未配置，默认返回 false。");
            }
        }
        catch { hasAttribute = false; }
        //Debug.Log($"物品检测结果：{hasAttribute}");
        onResult?.Invoke(hasAttribute);
    }
}