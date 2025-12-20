using System;
using UnityEngine;

/// <summary>
/// 容器 EntryPoint 示例：构造时订阅 IInventoryService 事件并记录日志
/// </summary>
public class InventoryLogger
{
    private readonly IInventoryService _inventory;

    public InventoryLogger(IInventoryService inventory)
    {
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        _inventory.OnItemChanged += OnItemChanged;
        Debug.Log("InventoryLogger 已构造并订阅事件（容器注入示例）");
    }

    private void OnItemChanged(ItemType t)
    {
        Debug.Log($"[InventoryLogger] OnItemChanged: {t}, Count: {_inventory.GetItemCount(t)}");
    }
}
