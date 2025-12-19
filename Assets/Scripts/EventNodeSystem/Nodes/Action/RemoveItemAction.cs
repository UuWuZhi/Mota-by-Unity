using System;
using UnityEngine;

[CreateAssetMenu(fileName = "RemoveItemAction", menuName = "EventNodes/Action/RemoveItem")]
public class RemoveAction : ActionNode
{
    public ItemType itemType;
    public int count = 1;

    public override void Execute(EventNodeContext ctx, Action onComplete)
    {
        // 简单调用背包接口
        if (ctx.PlayerInventory != null)
        {
            ctx.PlayerInventory.RemoveItem(itemType, count);
        }
        onComplete?.Invoke();
    }
}