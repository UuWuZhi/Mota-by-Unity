using System;
using System.Collections.Generic;

public interface IInventoryService
{
    event EventHandler<InventoryChangedEventArgs> InventoryChanged;
    void AddItem(ItemType type, int count = 1);
    bool RemoveItem(ItemType type, int count = 1);
    bool HasItem(ItemType type, int count = 1);
    int GetItemCount(ItemType type);
    void InitItemCounts();
    IReadOnlyList<InventoryEntry> GetEntries();
}