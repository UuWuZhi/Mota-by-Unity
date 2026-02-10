/// <summary>
/// 物品使用专用上下文，继承自通用 EventNodeContext。
/// 节点可以通过 MarkConsumed 标记物品应被消耗（由调用方负责实际移除）。
/// </summary>
public class ItemEventContext : EventNodeContext
{
    public ItemData ItemData { get; set; }
    public int InventorySlotIndex { get; set; } = -1;

    /// <summary>
    /// 标记在节点执行后物品应被消耗（调用者负责实际移除）。
    /// </summary>
    public bool Consumed { get; private set; } = false;

    public void MarkConsumed() => Consumed = true;

    public void ResetForReuse()
    {
        ItemData = null;
        InventorySlotIndex = -1;
        Consumed = false;
        // Vars 字典 由基类管理
    }
}
