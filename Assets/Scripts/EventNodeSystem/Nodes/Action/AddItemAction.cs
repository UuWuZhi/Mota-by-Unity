using System;
using UnityEngine;

[CreateAssetMenu(fileName = "AddItemAction", menuName = "EventNodes/Action/AddItem")]
[Serializable]
public class AddItemAction : ActionNode
{
    public ItemType itemType;
    public int count = 1;

    public override void Execute(EventNodeContext ctx, Action onComplete)
    {
        // 简单调用背包接口
        if (ctx.PlayerInventory != null)
        {
            ctx.PlayerInventory.AddItem(itemType, count);
        }
        onComplete?.Invoke();
    }
}