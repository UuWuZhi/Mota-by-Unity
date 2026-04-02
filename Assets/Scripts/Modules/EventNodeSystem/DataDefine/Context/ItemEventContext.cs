/// <summary>
/// 物品使用专用上下文，继承自通用 EventNodeContext。
/// 节点可以通过 MarkUseSucceeded 标记物品使用成功（由调用方负责实际移除或后续逻辑）。
/// </summary>
public class ItemEventContext : EventNodeContext
{
    public ItemData ItemData { get; set; }
    public bool UseSucceeded { get; private set; } = false; //表示物品使用是否成功
    public void MarkUseSucceeded() => UseSucceeded = true;

}
