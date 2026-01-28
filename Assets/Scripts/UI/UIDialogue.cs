using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using VContainer;

/// <summary>
/// TextMeshPro 版对话 UI 显示与交互
/// - 将选项的显示延后到文字完全打出之后
/// - 鼠标点击仍然可用，键盘输入由 InputManager 转发到本类的公有方法处理
/// </summary>
public class UIDialogue : BaseUI
{
    [Header("UI 元素")]
    [Tooltip("说话者文本")]   public TextMeshProUGUI speakerText;
    [Tooltip("对话文本")]     public TextMeshProUGUI contentText;
    [Tooltip("选项容器")]     public Transform choicesContainer;
    [Tooltip("选项按钮预制")] public Button choiceButtonPrefab;

    [Header("打字机参数")]
    [Tooltip("打字显示速度")] public float typeSpeed = 0.02f;

    private Coroutine _typeRoutine;             // 打字协程引用
    private DialogueData _currentDialogue;      // 持有当前对话数据

    private bool _typeFinished = false;         // 文字是否已经全部显示
    private bool _eventSubscribed = false;      // 是否已订阅事件
    private bool _awaitingContinue = false;     // 是否处于等待继续（无选项时）

    private readonly List<Button> _choiceButtons = new List<Button>();
    private int _selectedIndex = -1;

    private DialogueManager _dialogueManager;
    //==============================================================================//
    //                                                                              //
    //                                 生命周期                                     //
    //                                                                              //
    //==============================================================================//
    #region 生命周期
    /// <summary>
    /// Unity Start 回调。尝试订阅 DialogueManager 事件并在可用时注册 UI 根节点，初始隐藏 root。
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeDialogueManager();
    }

    /// <summary>
    /// Unity OnDestroy 回调。解除对 DialogueManager 事件的订阅并做清理。
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();
        UnsubscribeDialogueManager();
    }

    /// <summary>
    /// 通过依赖注入接收所需的管理器实例并初始化订阅与注册。
    /// </summary>
    /// <param name="dialogueManager">注入的 DialogueManager 实例。</param>
    /// <param name="uiManager">注入的 UIManager 实例。</param>
    [Inject]
    public void Inject(DialogueManager dialogueManager)
    {
        _dialogueManager = dialogueManager;
        SubscribeDialogueManager();
    }
    #endregion
    //==============================================================================//
    //                                                                              //
    //                                 事件系统                                     //
    //                                                                              //
    //==============================================================================//
    #region 事件系统
    /// <summary>
    /// 当对话开始时调用：显示对话 UI 根节点。
    /// </summary>
    private void OnDialogueStarted()
    {
        if (root != null) root.SetActive(true);
    }

    /// <summary>
    /// 当对话内容改变时调用。更新说话者文本、重置状态、停止旧的打字协程并开始新的打字，
    /// 清理旧的选项并根据新的对话数据创建新的选项（但初始为隐藏，打字完成后显示）。
    /// </summary>
    /// <param name="data">新的对话数据，可能包含文本、说话者和选项列表。</param>
    private void OnDialogueChanged(DialogueData data)
    {
        if (data == null) return;
        _currentDialogue = data;

        // 更新说话者文本（安全检查）
        if (speakerText != null)
            speakerText.text = string.IsNullOrEmpty(data.Speaker) ? string.Empty : data.Speaker;

        // 重置状态
        _typeFinished = false;
        _awaitingContinue = false;

        // 停止已有的打字协程并开始新的
        StopTypingRoutineIfAny();
        if (contentText != null)
            _typeRoutine = StartCoroutine(TypeTextRoutine(data.Text));

        // 清理旧的选项元素
        ClearChoices();

        // 构建新的选项（但初始为隐藏，等打字结束后显示）
        if (choicesContainer == null || choiceButtonPrefab == null)
        {
            // 无法创建按钮：直接返回，TypeTextRoutine 或 AfterTypeFinished_ShowChoicesOrContinue 会处理继续逻辑
            return;
        }

        if (data.Options != null && data.Options.Count > 0)
        {
            for (int i = 0; i < data.Options.Count; i++)
            {
                int idx = i; // closure safety
                var btn = InstantiateChoiceButton();
                if (btn == null) break;

                SetupChoiceButton(btn, data.Options[i]?.Text ?? string.Empty, idx);
            }
        }
        else
        {
            // 没有选项时创建一个 Continue 按钮，作为默认的继续交互
            var btn = InstantiateChoiceButton();
            if (btn != null)
            {
                SetupContinueButton(btn);
            }
        }
    }

    /// <summary>
    /// 当对话结束时调用。停止打字、重置内部状态、清理选项并隐藏 UI 根节点。
    /// </summary>
    private void OnDialogueEnded()
    {
        StopTypingRoutineIfAny();

        _typeFinished = false;
        _awaitingContinue = false;
        _currentDialogue = null;

        ClearChoices();

        if (root != null) root.SetActive(false);
    }

    /// <summary>
    /// 尝试将当前 root 注册到 UIManager，注册仅执行一次且在 _uiManager 与 root 可用时执行。
    /// </summary>
    //private void TryRegisterRoot()
    //{
    //    if (_registeredRoot) return;
    //    if (_uiManager == null || root == null) return;
    //    _uiManager.RegisterUIRoot(root);
    //    _registeredRoot = true;
    //}

    /// <summary>
    /// 订阅 DialogueManager 的对话相关事件（开始/改变/结束）。
    /// </summary>
    private void SubscribeDialogueManager()
    {
        if (_dialogueManager == null || _eventSubscribed) return;
        _dialogueManager.OnDialogueStarted += OnDialogueStarted;
        _dialogueManager.OnDialogueChanged += OnDialogueChanged;
        _dialogueManager.OnDialogueEnded += OnDialogueEnded;
        _eventSubscribed = true;
    }

    /// <summary>
    /// 取消订阅 DialogueManager 的对话相关事件，防止内存泄漏或重复订阅。
    /// </summary>
    private void UnsubscribeDialogueManager()
    {
        if (_dialogueManager == null || !_eventSubscribed) return;
        _dialogueManager.OnDialogueStarted -= OnDialogueStarted;
        _dialogueManager.OnDialogueChanged -= OnDialogueChanged;
        _dialogueManager.OnDialogueEnded -= OnDialogueEnded;
        _eventSubscribed = false;
    }
    #endregion
    //==============================================================================//
    //                                                                              //
    //                                 工具方法                                     //
    //                                                                              //
    //==============================================================================//
    #region 工具方法
    //==============================================================================//
    //                                 方法：导航                                   //
    //==============================================================================//
    /// <summary>
    /// 通过方向选择上/下个选项。
    /// </summary>
    /// <param name="direction">方向，通常为 +1（下一个）或 -1（上一个）。</param>
    public void Navigate(int direction)
    {
        if (!IsChoicesVisible() || _choiceButtons.Count == 0) return;
        int count = _choiceButtons.Count;
        if (count == 0) return;
        int next = (_selectedIndex + direction + count) % count;
        SetSelected(next);
    }
    //==============================================================================//
    //                                 方法：选择                                   //
    //==============================================================================//
    /// <summary>
    /// 设置选中索引并应用选择状态（对输入进行边界检查）。
    /// </summary>
    /// <param name="idx">要选中的索引。</param>
    private void SetSelected(int idx)
    {
        if (_choiceButtons.Count == 0) return;
        idx = Mathf.Clamp(idx, 0, _choiceButtons.Count - 1);
        _selectedIndex = idx;
        SelectButtonAt(_selectedIndex);
    }
    /// <summary>
    /// 将指定索引处的按钮设置为 EventSystem 的当前选中对象，以便响应键盘/手柄等输入。
    /// </summary>
    /// <param name="idx">要选中的按钮索引。</param>
    private void SelectButtonAt(int idx)
    {
        if (_choiceButtons.Count == 0) return;
        var btn = _choiceButtons[Mathf.Clamp(idx, 0, _choiceButtons.Count - 1)];
        if (btn != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(btn.gameObject);
        }
    }
    //==============================================================================//
    //                                 方法：确认                                   //
    //==============================================================================//
    /// <summary>
    /// 确认交互：如果正在打字则跳过并显示完整文本；否则根据当前 UI 状态触发选项选择或继续事件。
    /// </summary>
    public void Confirm()
    {
        if (_typeRoutine != null)
        {
            StopAndFinalizeTyping();
            return;
        }

        if (IsChoicesVisible() && _choiceButtons.Count > 0)
        {
            int idx = Mathf.Clamp(_selectedIndex, 0, _choiceButtons.Count - 1);
            // 通过 DialogueManager 回调外部系统
            if (_dialogueManager != null)
            {
                _dialogueManager.NotifyUIChoiceSelected(idx);
            }
            return;
        }

        if (_awaitingContinue)
        {
            _awaitingContinue = false;
            // 通过 DialogueManager 回调通知继续
            if (_dialogueManager != null)
            {
                _dialogueManager.NotifyUIContinue();
            }
        }
    }
    //==============================================================================//
    //                                 方法：打字                                   //
    //==============================================================================//
    /// <summary>
    /// 打字协程：逐字符显示文本，完成后设置相关状态并调用后续显示逻辑。
    /// </summary>
    /// <param name="text">要显示的完整文本。</param>
    /// <returns>IEnumerator，用于 Unity 的协程调度。</returns>
    private IEnumerator TypeTextRoutine(string text)
    {
        if (contentText == null)
            yield break;

        contentText.text = string.Empty;

        if (string.IsNullOrEmpty(text))
        {
            _typeRoutine = null;
            _typeFinished = true;

            // 如果没有选项，则进入等待继续的状态
            if (_currentDialogue != null && _currentDialogue.IsEnd())
                _awaitingContinue = true;

            AfterTypeFinished_ShowChoicesOrContinue();
            yield break;
        }

        for (int i = 1; i <= text.Length; i++)
        {
            contentText.text = text.Substring(0, i);
            yield return new WaitForSeconds(typeSpeed);
        }

        _typeRoutine = null;
        _typeFinished = true;

        AfterTypeFinished_ShowChoicesOrContinue();
    }
    /// <summary>
    /// 在打字完成后执行：显示已创建的选项按钮或进入等待继续状态（当无选项时）。
    /// </summary>
    private void AfterTypeFinished_ShowChoicesOrContinue()
    {
        if (_choiceButtons.Count == 0)
        {
            _awaitingContinue = true;
            return;
        }

        // 激活所有按钮（之前为隐藏）
        for (int i = 0; i < _choiceButtons.Count; i++)
        {
            var b = _choiceButtons[i];
            if (b != null) b.gameObject.SetActive(true);
        }

        // 默认选择第一个
        _selectedIndex = 0;
        SelectButtonAt(_selectedIndex);

        // 如果当前对话没有选项，则仍然处于等待继续状态
        if (_currentDialogue != null && _currentDialogue.IsEnd())
        {
            _awaitingContinue = true;
        }
        else
        {
            _awaitingContinue = false;
        }
    }
    /// <summary>
    /// 停止并清理正在运行的打字协程（不显示完整文本，仅停止协程）。
    /// </summary>
    private void StopTypingRoutineIfAny()
    {
        if (_typeRoutine != null)
        {
            StopCoroutine(_typeRoutine);
            _typeRoutine = null;
        }
    }
    /// <summary>
    /// 停止打字协程并直接将完整文本显示出来，随后触发打字完成后的显示逻辑。
    /// </summary>
    private void StopAndFinalizeTyping()
    {
        StopTypingRoutineIfAny();
        if (_currentDialogue != null && contentText != null)
        {
            contentText.text = _currentDialogue.Text;
        }
        _typeFinished = true;
        AfterTypeFinished_ShowChoicesOrContinue();
    }
    //==============================================================================//
    //                                 方法：按钮                                   //
    //==============================================================================//
    /// <summary>
    /// 清除 choicesContainer 下的所有子对象并清空内部记录，同时重置选中索引。
    /// </summary>
    private void ClearChoices()
    {
        if (choicesContainer != null)
        {
            // 使用 for 循环避免在枚举时修改集合
            for (int i = choicesContainer.childCount - 1; i >= 0; i--)
            {
                var child = choicesContainer.GetChild(i);
                if (child != null) Destroy(child.gameObject);
            }
        }

        _choiceButtons.Clear();
        _selectedIndex = -1;
    }
    /// <summary>
    /// 实例化一个按钮并加入到内部列表，按钮初始为隐藏。
    /// </summary>
    /// <returns>返回新创建的 Button 实例，若无法创建则为 null。</returns>
    private Button InstantiateChoiceButton()
    {
        if (choiceButtonPrefab == null || choicesContainer == null)
            return null;

        var btn = Instantiate(choiceButtonPrefab, choicesContainer);
        btn.onClick.RemoveAllListeners();
        btn.gameObject.SetActive(false);
        _choiceButtons.Add(btn);
        return btn;
    }
    /// <summary>
    /// 将按钮与具体选项文本与回调关联。
    /// </summary>
    /// <param name="btn">要设置的按钮实例。</param>
    /// <param name="label">按钮显示文本。</param>
    /// <param name="idx">选项索引，用于回调传递。</param>
    private void SetupChoiceButton(Button btn, string label, int idx)
    {
        if (btn == null) return;

        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = label ?? string.Empty;
        else Debug.LogWarning("DialogueUI: choiceButtonPrefab 缺少 TextMeshProUGUI");

        // 使用独立方法处理点击逻辑，便于复用和测试
        btn.onClick.AddListener(() => OnChoiceButtonClicked(idx));
    }
    /// <summary>
    /// 点击选项按钮时的处理：如果仍在打字则先显示完整文本，否则通过 DialogueManager 通知选择。
    /// </summary>
    /// <param name="idx">被点击的选项索引。</param>
    private void OnChoiceButtonClicked(int idx)
    {
        if (_typeRoutine != null)
        {
            StopAndFinalizeTyping();
        }
        else
        {
            if (_dialogueManager != null)
            {
                _dialogueManager.NotifyUIChoiceSelected(idx);
            }
        }
    }
    /// <summary>
    /// 将按钮设置为 "Continue" 并绑定继续按钮的回调。
    /// </summary>
    /// <param name="btn">要设置为继续按钮的实例。</param>
    private void SetupContinueButton(Button btn)
    {
        if (btn == null) return;
        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = "Continue";

        btn.onClick.AddListener(OnContinueButtonClicked);
    }
    /// <summary>
    /// Continue 按钮被点击时的处理：如果仍在打字则先显示完整文本，否则通知 DialogueManager 继续流程。
    /// </summary>
    private void OnContinueButtonClicked()
    {
        if (_typeRoutine != null)
        {
            StopAndFinalizeTyping();
        }
        else
        {
            _awaitingContinue = false;
            if (_dialogueManager != null)
            {
                _dialogueManager.NotifyUIContinue();
            }
        }
    }
    //==============================================================================//
    //                                 方法：接口                                   //
    //==============================================================================//
    public bool IsTypeFinished()
    {
        return _typeFinished;
    }
    public bool IsAwaitingContinue()
    {
        return _awaitingContinue;
    }
    public bool IsChoicesVisible()
    {
        return _choiceButtons.Count > 0 && _choiceButtons[0] != null && _choiceButtons[0].gameObject.activeSelf;
    }
    #endregion
}