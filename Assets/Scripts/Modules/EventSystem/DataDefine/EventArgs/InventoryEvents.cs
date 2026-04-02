using System;

public class InventoryChangedEventArgs : EventArgs
{
    // 哪种道具发生变化；使用 ItemType.All 表示全量更新
    public ItemType ChangedType { get; set; }
    // 可选：数量变化（正为增加，负为减少），调用方可选提供
    public int Delta { get; set; }

    public InventoryChangedEventArgs() { }
    public InventoryChangedEventArgs(ItemType type, int delta = 0)
    {
        ChangedType = type;
        Delta = delta;
    }
}
