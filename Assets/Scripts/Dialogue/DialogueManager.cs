/// <summary>
/// 简化的对话管理器：仅负责展示对话数据并广播当前节点变更/对话开始/结束事件。
/// 对话的推进与选项处理由外部系统（Node 系统 / 控制器）负责调用 SetCurrentNode / EndDialogue。
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    public bool IsActive { get; private set; } = false;
    private DialogueNode _currentNode;
    public event Action OnDialogueStarted;
    public event Action<DialogueNode> OnNodeChanged;
    public event Action OnDialogueEnded;
    //==============================================================================//
    //                                                                              //
    //                                 生命周期                                     //
    //                                                                              //
    //==============================================================================//
    #region 生命周期
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }
    #endregion
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
    public void StartDialogue(DialogueNode startNode)
    {
        if (startNode == null) return;
        _currentNode = startNode;
        IsActive = true;
        OnDialogueStarted?.Invoke();
        PublishCurrentNode();
    }
    /// <summary>
    /// 结束当前对话（外部可调用）
    /// </summary>
    public void EndDialogue()
    {
        IsActive = false;
        OnDialogueEnded?.Invoke();
        _currentNode = null;
    }
    private void PublishCurrentNode()
    {
        OnNodeChanged?.Invoke(_currentNode);
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
    /// 现在会监听场景中的 DialogueUI 的 OnContinueClicked 并在玩家点击继续时调用 EndDialogue，
    /// 以保证 OnDialogueEnded 被触发从而执行 onComplete。
    /// </summary>
    public void Show(string text, string speaker, Action onComplete)
    {
        if (string.IsNullOrEmpty(text))
        {
            onComplete?.Invoke();
            return;
        }

        var node = new DialogueNode
        {
            Id = "tmp_0",
            Speaker = speaker,
            Text = text,
            Choices = new List<DialogueChoice>(),
            NextNodeId = null
        };

        DialogueUI ui = GameObject.FindObjectOfType<DialogueUI>();

        Action uiContinueHandler = null;
        Action handler = null;

        handler = () =>
        {
            // 当对话结束时，取消两端订阅并执行回调
            OnDialogueEnded -= handler;
            if (ui != null && uiContinueHandler != null)
                ui.OnContinueClicked -= uiContinueHandler;

            onComplete?.Invoke();
        };
        OnDialogueEnded += handler;

        // 如果有 UI，则让 DialogueManager 监听一次 OnContinueClicked，
        // 在用户点击继续时由 DialogueManager 调用 EndDialogue()
        if (ui != null)
        {
            uiContinueHandler = () =>
            {
                // 防止重复调用，先取消订阅再结束对话
                if (ui != null && uiContinueHandler != null)
                    ui.OnContinueClicked -= uiContinueHandler;

                EndDialogue();
            };
            ui.OnContinueClicked += uiContinueHandler;
        }

        StartDialogue(node);
    }

    /// <summary>
    /// 外部主动设置当前节点（例如 Node 系统在执行动作后调用以推进到指定节点）
    /// 返回 true 表示成功设置并已广播节点变更。
    /// </summary>
    public bool SetCurrentNode(DialogueNode node)
    {
        if (node == null) return false;
        _currentNode = node;
        PublishCurrentNode();
        return true;
    }
    #endregion
}