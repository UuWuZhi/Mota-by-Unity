/// <summary>
/// 物品使用专用上下文，继承自通用 EventNodeContext。
/// 节点可以通过 MarkUseSucceeded 标记物品使用成功（由调用方负责实际移除或后续逻辑）。
/// </summary>
public class ItemEventContext : EventNodeContext
{
    public ItemData ItemData { get; set; }
    /// <summary>
    /// 表示物品使用是否成功（节点执行结果）。
    /// 使用下划线前缀以突出这是上下文内的运行时标志。
    /// 节点应调用 MarkUseSucceeded() 来标记成功。
    /// </summary>
    public bool UseSucceeded { get; private set; } = false;

    public void MarkUseSucceeded() => UseSucceeded = true;

}
