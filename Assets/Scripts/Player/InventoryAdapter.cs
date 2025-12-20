using System;

/// <summary>
/// 将现有的 PlayerInventory 适配为可注入的接口实现，最小改动、便于测试与替换。
/// 通过构造注入 PlayerInventory（容器会注入之前用 RegisterInstance 注册的实例）。
/// </summary>
public class InventoryAdapter : IInventoryService
{
    private readonly PlayerInventory _inventory;

    public event Action<ItemType> OnItemChanged;

    public InventoryAdapter(PlayerInventory inventory)
    {
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        // 将底层事件转发到接口事件，保持订阅兼容
        _inventory.OnItemChanged += args => OnItemChanged?.Invoke(args);
    }

    public void AddItem(ItemType type, int count = 1) => _inventory.AddItem(type, count);
    public bool RemoveItem(ItemType type, int count = 1) => _inventory.RemoveItem(type, count);
    public bool HasItem(ItemType type, int count = 1) => _inventory.HasItem(type, count);
    public int GetItemCount(ItemType type) => _inventory.GetItemCount(type);
}