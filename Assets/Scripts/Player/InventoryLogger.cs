using System;
using UnityEngine;

/// <summary>
/// 只是一个容器 EntryPoint，用来展示通过构造注入 IInventoryService 的最小使用示例。
/// 容器会在启动时构造本对象并注入依赖（RegisterEntryPoint）。
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
        Debug.Log($"[InventoryLogger] OnItemChanged: {t}, 当前数量: {_inventory.GetItemCount(t)}");
    }
}