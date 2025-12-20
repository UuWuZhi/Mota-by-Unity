using System;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerHasItemCondition", menuName = "EventNodes/Condition/PlayerHasItem")]
public class PlayerHasItemCondition : ConditionNode
{
    public ItemType itemType;
    public int requiredCount = 1;

    public override void Evaluate(EventNodeContext ctx, Action<bool> onResult)
    {
        bool hasItem = false;
        try
        {
            if (ctx.InventoryService != null)
            {
                hasItem = ctx.InventoryService.HasItem(itemType, requiredCount);
            }
            else
            {
                Debug.LogError("PlayerHasItemCondition: InventoryService 未配置，无法判断道具数量。请确保 InventoryAdapter 已通过容器注册。");
            }
        }
        catch { hasItem = false; }
        onResult?.Invoke(hasItem);
    }
}