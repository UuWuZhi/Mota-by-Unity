using System;
using UnityEngine;

[CreateAssetMenu(fileName = "RemoveItemAction", menuName = "EventNodes/Action/RemoveItem")]
public class RemoveAction : ActionNode
{
    public ItemType itemType;
    public int count = 1;

    public override void Execute(EventNodeContext ctx, Action onComplete)
    {
        // 使用接口服务进行移除
        if (ctx.InventoryService != null)
        {
            ctx.InventoryService.RemoveItem(itemType, count);
            Debug.Log("使用 InventoryService 移除道具。");
        }
        else
        {
            Debug.LogError("RemoveAction: InventoryService 未配置，无法移除道具。请确保 InventoryAdapter 已通过容器注册。");
        }
        onComplete?.Invoke();
    }
}