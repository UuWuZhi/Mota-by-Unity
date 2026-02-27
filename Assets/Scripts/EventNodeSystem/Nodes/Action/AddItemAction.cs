using System;
using UnityEngine;

[CreateAssetMenu(fileName = "AddItemAction", menuName = "EventNodes/Action/AddItem")]
[Serializable]
public class AddItemAction : ActionNode
{
    public ItemType itemType;
    public int count = 1;

    public override Type[] GetRequiredServices()
    {
        return new[] { typeof(IInventoryService) };
    }

    public override void Execute(EventNodeContext ctx, Action onComplete)
    {
        // 强制使用 InventoryService；如果不可用则记录错误并直接完成（避免隐式回退）
        var inventoryService = ctx.GetService<IInventoryService>();
        if (inventoryService != null)
        {
            inventoryService.AddItem(itemType, count);
            Debug.Log("使用 InventoryService 添加道具。");
        }
        else
        {
            Debug.LogError("AddItemAction: InventoryService 未配置，无法添加道具。请确保 InventoryAdapter 已通过容器注册。");
        }
        onComplete?.Invoke();
    }
}