using System;
using UnityEngine;

/// <summary>
/// EntryPoint 用于在容器构建时记录 IInventoryService 事件（迁移：使用 EventCenter 订阅）
/// </summary>
public class InventoryLogger
{
    private readonly IInventoryService _inventory;

    public InventoryLogger(IInventoryService inventory)
    {
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        // 订阅全局 EventCenter 的库存变更事件
        if (EventCenter.Instance != null)
            EventCenter.Instance.OnInventoryChanged += OnInventoryChanged;

        Debug.Log("InventoryLogger 已注册库存变更监听（EventCenter）");
    }

    private void OnInventoryChanged(object sender, InventoryChangedEventArgs args)
    {
        if (args == null) return;
        Debug.Log($"[InventoryLogger] InventoryChanged: {args.ChangedType}, Delta: {args.Delta}, Count: {_inventory.GetItemCount(args.ChangedType)}");
    }
}
