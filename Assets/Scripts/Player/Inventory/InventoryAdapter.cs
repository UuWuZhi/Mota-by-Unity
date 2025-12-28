using System;
using UnityEngine;
using VContainer;

/// <summary>
/// 把现有的 PlayerInventory 适配为 IInventoryService 实现
/// 兼容迁移期间场景中挂载的 PlayerInventory：通过延迟查找（FindObjectOfType）来避免容器强制解析 PlayerInventory
/// </summary>
public class InventoryAdapter : IInventoryService
{
    private PlayerInventory _inventory;

    // 无参构造（容器可以用这个构造创建实例）
    public InventoryAdapter()
    {
    }

    // 容器注入时调用，但不声明 PlayerInventory 参数，避免容器尝试解析该类型
    [Inject]
    public void Construct()
    {
        TryAttachToSceneInventory();
    }

    // 尝试查找并附着到场景中的 PlayerInventory（如果存在）
    private void TryAttachToSceneInventory()
    {
        if (_inventory != null) return;
        var found = GameObject.FindObjectOfType<PlayerInventory>();
        if (found != null)
        {
            _inventory = found;
            // PlayerInventory 现在通过 EventCenter 广播变更，不需要订阅本地事件
        }
    }

    // 确保在每次公开 API 调用前尝试附着（容器构建顺序差异期间的安全措施）
    private void EnsureInventory()
    {
        if (_inventory == null)
            TryAttachToSceneInventory();
    }

    public void AddItem(ItemType type, int count = 1)
    {
        EnsureInventory();
        if (_inventory == null) return;
        _inventory.AddItem(type, count);
    }
    public bool RemoveItem(ItemType type, int count = 1)
    {
        EnsureInventory();
        return _inventory != null && _inventory.RemoveItem(type, count);
    }
    public bool HasItem(ItemType type, int count = 1)
    {
        EnsureInventory();
        return _inventory != null && _inventory.HasItem(type, count);
    }
    public int GetItemCount(ItemType type)
    {
        EnsureInventory();
        return _inventory != null ? _inventory.GetItemCount(type) : 0;
    }
    public void InitItemCounts()
    {
        EnsureInventory();
        _inventory?.InitItemCounts();
    }
}