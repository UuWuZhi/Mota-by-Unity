using System;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerHasItemCondition", menuName = "EventNodes/Condition/PlayerHasItem")]
public class PlayerHasItemCondition : ConditionNode
{
    public ItemType itemType;
    public int requiredCount = 1;

    public override void Evaluate(EventNodeContext ctx, Action<bool> onResult)
    {
        //Debug.Log("Node:检测物品开始");
        bool hasItem;
        try
        {
            hasItem = ctx.PlayerInventory != null && ctx.PlayerInventory.HasItem(itemType, requiredCount);
        }
        catch { hasItem = false; }
        //Debug.Log($"物品检测结果：{hasItem}");
        onResult?.Invoke(hasItem);
    }
}