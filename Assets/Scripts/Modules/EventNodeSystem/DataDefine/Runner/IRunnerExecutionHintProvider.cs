namespace Modules.EventNodeSystem.DataDefine.Runner
{
    /// <summary>
    ///     节点执行提示提供器。
    ///     节点可通过该接口向 Runner 声明预期执行形态。
    /// </summary>
    public interface IRunnerExecutionHintProvider
    {
        /// <summary>
        ///     获取执行提示。
        /// </summary>
        /// <returns>当前节点的执行提示。</returns>
        RunnerExecutionHint GetExecutionHint();
    }
}