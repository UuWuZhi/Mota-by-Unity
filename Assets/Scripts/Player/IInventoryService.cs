using System;

public interface IInventoryService
{
    event Action<ItemType> OnItemChanged;
    void AddItem(ItemType type, int count = 1);
    bool RemoveItem(ItemType type, int count = 1);
    bool HasItem(ItemType type, int count = 1);
    int GetItemCount(ItemType type);
    void InitItemCounts();
}