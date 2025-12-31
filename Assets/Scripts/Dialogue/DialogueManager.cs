using System;
using System.Collections.Generic;
using VContainer;

/// <summary>
/// 简化的对话管理器：仅负责展示对话数据并广播当前节点变更/对话开始/结束事件。
/// 对话的推进与选项处理由外部系统（Node 系统 / 控制器）负责调用 SetCurrentDialogue / EndDialogue。
/// 现在为纯 C# 类，由容器管理单例生命周期。
/// </summary>
public class DialogueManager
{
    private DialogueData _currentDialogue;

    public event Action OnDialogueStarted;
    public event Action<DialogueData> OnDialogueChanged;
    public event Action OnDialogueEnded;
    public event Action<int> OnUIChoiceSelected;

    private readonly IGlobalEventVariables _globalEventVariables;

    private bool _captureNextContinue = false;
    public bool IsActive = false;

    [Inject]
    public DialogueManager(IGlobalEventVariables globalEventVariables)
    {
        _globalEventVariables = globalEventVariables;
        _globalEventVariables?.SetBool(GlobalEventKey.DialogueIsActive, IsActive);
    }

    //==============================================================================//
    //                                                                              //
    //                                 对话进程                                     //
    //                                                                              //
    //==============================================================================//
    #region 对话进程
    /// <summary>
    /// 启动对话（直接传入起始 DialogueNode）。
    /// 对话推进仍由外部控制（通常通过 SetCurrentNode 或 EndDialogue）。
    /// </summary>
    public void StartDialogue(DialogueData startData)
    {
        if (startData == null) return;
        _currentDialogue = startData;
        IsActive = true;
        SyncGlobalDialogueActive();
        OnDialogueStarted?.Invoke();
        PublishCurrentDialogue();
    }

    /// <summary>
    /// 结束当前对话（外部可调用）
    /// </summary>
    public void EndDialogue()
    {
        IsActive = false;
        SyncGlobalDialogueActive();
        OnDialogueEnded?.Invoke();
        _currentDialogue = null;
    }

    private void PublishCurrentDialogue()
    {
        OnDialogueChanged?.Invoke(_currentDialogue);
    }

    /// <summary>
    /// 外部主动设置当前节点（例如 Node 系统在执行动作后调用以推进到指定节点）
    /// 返回 true 表示成功设置并已广播节点变更。
    /// </summary>
    public bool SetCurrentDialogue(DialogueData data)
    {
        if (data == null) return false;
        _currentDialogue = data;
        PublishCurrentDialogue();
        return true;
    }
    #endregion
    //==============================================================================//
    //                                                                              //
    //                                 外部接口                                     //
    //                                                                              //
    //==============================================================================//
    #region 外部接口
    /// <summary>
    /// 运行时简便 API：显示单段文字并在结束时回调（内部创建临时 DialogueNode）。
    /// 对话结束回调由 onComplete 接收；对话推进仍由外部控制（通常临时对话无选项，结束即回调）。
    /// 现在不会再通过查找 UIDialogue 并订阅它的事件来控制流程，
    /// 而是通过内部标志捕获下一次 UI 的 Continue 操作（由 UIDialogue 调用 NotifyUIContinue），
    /// 并在对话结束时触发 onComplete。
    /// </summary>
    public void Show(string text, string speaker, Action onComplete)
    {
        if (string.IsNullOrEmpty(text))
        {
            onComplete?.Invoke();
            return;
        }

        var data = new DialogueData
        {
            Id = "tmp_0",
            Speaker = speaker,
            Text = text,
            Options = new List<DialogueOption>()
        };

        // 标记捕获下一次 UI 的 Continue 操作，UI 会通过 NotifyUIContinue 调用 EndDialogue
        _captureNextContinue = true;

        Action handler = null;

        handler = () =>
        {
            // 当对话结束时，取消订阅并执行回调
            OnDialogueEnded -= handler;
            _captureNextContinue = false;
            onComplete?.Invoke();
        };
        OnDialogueEnded += handler;

        StartDialogue(data);
    }
    #endregion
    //==============================================================================//
    //                                                                              //
    //                                 回调接口                                     //
    //                                                                              //
    //==============================================================================//
    #region 回调接口
    /// <summary>
    /// 由 UI 在用户点击 Continue 时调用。仅在内部要求捕获下一次 Continue 时会触发 EndDialogue。
    /// </summary>
    public void NotifyUIContinue()
    {
        if (!_captureNextContinue) return;
        _captureNextContinue = false;
        EndDialogue();
    }

    /// <summary>
    /// 由 UI 在用户点击选项时调用。会将选择通过 OnUIChoiceSelected 事件广播给外部订阅者（如 Node 系统）。
    /// </summary>
    public void NotifyUIChoiceSelected(int index)
    {
        OnUIChoiceSelected?.Invoke(index);
    }
    #endregion
    //==============================================================================//
    //                                                                              //
    //                                 简单方法                                     //
    //                                                                              //
    //==============================================================================//
    #region 简单方法
    private void SyncGlobalDialogueActive()
    {
        _globalEventVariables?.SetBool(GlobalEventKey.DialogueIsActive, IsActive);
    }
    #endregion
}