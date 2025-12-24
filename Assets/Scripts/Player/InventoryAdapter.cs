using System;
using UnityEngine;
using VContainer;

/// <summary>
/// 将现有的 PlayerInventory 适配为可注入的接口实现。
/// 现在使用构造注入获得场景中已注册的 PlayerInventory 实例（由 DiBootstrap.RegisterInstance 提供）。
/// 移除了全局静态 Current，鼓励通过容器解析 IInventoryService。
/// </summary>
public class InventoryAdapter : IInventoryService
{
    private readonly PlayerInventory _inventory;

    public event Action<ItemType> OnItemChanged;

    // 使用构造注入：容器应当 RegisterInstance(PlayerInventory.Instance)
    [Inject]
    public InventoryAdapter(PlayerInventory inventory)
    {
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        _inventory.OnItemChanged += args => OnItemChanged?.Invoke(args);
    }

    public void AddItem(ItemType type, int count = 1)
    {
        if (_inventory == null) return;
        _inventory.AddItem(type, count);
    }
    public bool RemoveItem(ItemType type, int count = 1)
    {
        return _inventory != null && _inventory.RemoveItem(type, count);
    }
    public bool HasItem(ItemType type, int count = 1)
    {
        return _inventory != null && _inventory.HasItem(type, count);
    }
    public int GetItemCount(ItemType type)
    {
        return _inventory != null ? _inventory.GetItemCount(type) : 0;
    }
    public void InitItemCounts()
    {
        _inventory?.InitItemCounts();
    }
}