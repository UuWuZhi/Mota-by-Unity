using System;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerHasItem", menuName = "EventNodes/Condition/PlayerHasItem")]
public class PlayerHasItem : ConditionNode
{
    public ItemType itemType;
    public int requiredCount = 1;

    public override Type[] GetRequiredServices()
    {
        return new[] { typeof(IInventoryService) };
    }

    public override void Evaluate(EventNodeContext ctx, Action<bool> onResult)
    {
        bool hasItem = false;
        try
        {
            var inventoryService = ctx?.GetService<IInventoryService>();
            if (inventoryService != null)
            {
                hasItem = inventoryService.HasItem(itemType, requiredCount);
            }
            else
            {
                Debug.LogError("PlayerHasItem: InventoryService 未配置，无法判断玩家物品数量。");
            }
        }
        catch { hasItem = false; }
        onResult?.Invoke(hasItem);
    }
}