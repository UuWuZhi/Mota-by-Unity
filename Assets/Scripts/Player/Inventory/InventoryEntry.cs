using System;

[Serializable]
public class InventoryEntry
{
    public ItemType Type;
    public int Count;

    public InventoryEntry() { }
    public InventoryEntry(ItemType type, int count)
    {
        Type = type;
        Count = count;
    }
}
