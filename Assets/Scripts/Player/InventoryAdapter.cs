using System;
using UnityEngine;
using VContainer;

/// <summary>
/// 将现有的 PlayerInventory 适配为可注入的接口实现，最小改动、便于测试与替换。
/// 支持无参构造：在容器构造或手动构造时若未传入 PlayerInventory，则尝试在场景中查找对应组件。
/// </summary>
public class InventoryAdapter : IInventoryService
{
    private readonly PlayerInventory _inventory;

    public static IInventoryService Current { get; private set; }

    public event Action<ItemType> OnItemChanged;

    // 无参构造：在运行时尝试查找场景中的 PlayerInventory 组件
    [Inject]
    public InventoryAdapter()
    {
        _inventory = GameObject.FindObjectOfType<PlayerInventory>();
        if (_inventory == null)
        {
            Debug.LogError("InventoryAdapter: 未找到 PlayerInventory 实例，库存服务不可用。");
            Current = null;
            return;
        }

        _inventory.OnItemChanged += args => OnItemChanged?.Invoke(args);
        Current = this;
    }

    // 仍保留带参构造以便于手工创建或测试时注入
    public InventoryAdapter(PlayerInventory inventory)
    {
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        _inventory.OnItemChanged += args => OnItemChanged?.Invoke(args);
        Current = this;
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