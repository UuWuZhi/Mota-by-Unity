namespace Modules.EventNodeSystem.DataDefine.Runner
{
    /// <summary>
    ///     节点执行提示：用于告知 Runner 当前节点的预期完成方式。
    /// </summary>
    public enum RunnerExecutionHint
    {
        /// <summary>
        ///     默认值：未知提示，Runner 按异步阻塞策略处理。
        /// </summary>
        Unknown = 0,

        /// <summary>
        ///     预计同步立即完成。
        /// </summary>
        SyncImmediate = 1,

        /// <summary>
        ///     预计异步阻塞完成。
        /// </summary>
        AsyncBlocking = 2
    }
}