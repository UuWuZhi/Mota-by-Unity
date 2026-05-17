namespace Modules.Core.DataDefine
{
    /// <summary>
    ///     标识全局事件/状态系统中的键，用于表示和区分可观察或可控的全局状态（例如当前层数、对话活动、UI 状态等）。
    /// </summary>
    /// <remarks>作为事件分发、状态查询与条件控制的统一键集合，便于模块间通信与状态同步。</remarks>
    public enum GlobalEventKey
    {
        LayerId, // 当前所在层数
        DialogueIsActive, // 对话系统是否正在运行（控制玩家输入等）
        UIState // 当前UI状态（如InventoryOpen等，控制玩家输入等）
    }
}