using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// TextMeshPro 版对话 UI 显示与交互
/// - 将选项的显示延后到文字完全打出之后
/// - 鼠标点击仍然可用，键盘输入由 InputManager 转发到本类的公有方法处理
/// </summary>
public class UIDialogue : MonoBehaviour
{
    [Header("UI 元素")]
    [Tooltip("对话根节点（用于整体显示/隐藏）")]
    public GameObject root;
    [Tooltip("说话者文本")]
    public TextMeshProUGUI speakerText;
    [Tooltip("对话内容 TextMeshProUGUI")]
    public TextMeshProUGUI contentText;
    [Tooltip("选项容器（直接把按钮放在这里）")]
    public Transform choicesContainer;
    [Tooltip("选项按钮预制（Button + TextMeshProUGUI）")]
    public Button choiceButtonPrefab;

    [Header("打字机参数")]
    public float typeSpeed = 0.02f;

    private DialogueManager _dm;
    private Coroutine _typeRoutine; // 打字协程引用

    private DialogueNode _currentNode;

    // 状态
    private bool _typeFinished = false;       // 文字是否已经全部显示
    public bool TypeFinished => _typeFinished;
    private bool _awaitingContinue = false;   // 是否处于等待继续（无选项时）

    // 选项按钮管理（在 OnNodeChanged 创建，但初始为隐藏，直到文字打完再显示）
    private readonly List<Button> _choiceButtons = new List<Button>();
    private int _selectedIndex = -1;

    // 外部事件（节点系统订阅）
    public event Action<int> OnChoiceClicked;
    public event Action OnContinueClicked;

    private void Start()
    {
        _dm = DialogueManager.Instance;
        if (_dm == null)
        {
            Debug.LogWarning("DialogueUI: 未找到 DialogueManager，UI 将不会工作。");
            return;
        }
        _dm.OnDialogueStarted += OnDialogueStarted;
        _dm.OnNodeChanged += OnNodeChanged;
        _dm.OnDialogueEnded += OnDialogueEnded;

        if (root != null) root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_dm == null) return;
        _dm.OnDialogueStarted -= OnDialogueStarted;
        _dm.OnNodeChanged -= OnNodeChanged;
        _dm.OnDialogueEnded -= OnDialogueEnded;
    }

    // 暴露给 InputManager 的只读状态/控制方法
    public bool ChoicesVisible => _choiceButtons.Count > 0 && _choiceButtons[0].gameObject.activeSelf;
    public bool IsAwaitingContinue => _awaitingContinue;

    // 导航：direction = +1 向下/右，-1 向上/左
    public void Navigate(int direction)
    {
        if (!ChoicesVisible || _choiceButtons.Count == 0) return;
        int count = _choiceButtons.Count;
        if (count == 0) return;
        int next = (_selectedIndex + direction + count) % count;
        SetSelected(next);
    }

    // 确认：如果还在打字则立即完成打字；否则触发当前选项或继续事件
    public void Confirm()
    {
        // 如果正在打字，立即跳过到全文并显示选项（或进入等待继续）
        if (_typeRoutine != null)
        {
            StopCoroutine(_typeRoutine);
            _typeRoutine = null;
            if (_currentNode != null && contentText != null) contentText.text = _currentNode.Text;
            _typeFinished = true;

            // 文字直接完成，显示选项 / 进入等待继续
            AfterTypeFinished_ShowChoicesOrContinue();
            return;
        }

        // 如果已经显示选项，则触发选中项
        if (ChoicesVisible && _choiceButtons.Count > 0)
        {
            int idx = Mathf.Clamp(_selectedIndex, 0, _choiceButtons.Count - 1);
            // 触发事件并由外部系统处理（与鼠标点击一致）
            OnChoiceClicked?.Invoke(idx);
            return;
        }

        // 否则为没有选项的继续
        if (_awaitingContinue)
        {
            _awaitingContinue = false;
            OnContinueClicked?.Invoke();
        }
    }

    private void OnDialogueStarted()
    {
        if (root != null) root.SetActive(true);
    }

    private void OnNodeChanged(DialogueNode node)
    {
        if (node == null) return;
        _currentNode = node;

        // 更新说话者
        if (speakerText != null)
            speakerText.text = string.IsNullOrEmpty(node.Speaker) ? "" : node.Speaker;

        // 重置状态
        _typeFinished = false;
        _awaitingContinue = false;

        // 停止之前的打字
        if (_typeRoutine != null) StopCoroutine(_typeRoutine);
        _typeRoutine = StartCoroutine(TypeTextRoutine(node.Text));

        // 清理旧选项
        foreach (Transform t in choicesContainer) Destroy(t.gameObject);
        _choiceButtons.Clear();
        _selectedIndex = -1;

        // 预先创建选项按钮，但先保持隐藏，直到文字完全显示
        if (choicesContainer != null && choiceButtonPrefab != null)
        {
            if (node.Choices != null && node.Choices.Count > 0)
            {
                for (int i = 0; i < node.Choices.Count; i++)
                {
                    int idx = i;
                    var btn = Instantiate(choiceButtonPrefab, choicesContainer);
                    btn.onClick.RemoveAllListeners();
                    btn.gameObject.SetActive(false); // 重要：先隐藏，等文字完全打出后再显示
                    _choiceButtons.Add(btn);

                    var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmp != null) tmp.text = node.Choices[i].Text;
                    else Debug.LogWarning("DialogueUI: choiceButtonPrefab 缺少 TextMeshProUGUI");

                    btn.onClick.AddListener(() =>
                    {
                        // 鼠标点击的行为与键盘一致：
                        if (_typeRoutine != null)
                        {
                            // 点击时若仍在打字：立即完成并显示选项（但不立刻触发选项）
                            StopCoroutine(_typeRoutine);
                            _typeRoutine = null;
                            if (_currentNode != null && contentText != null) contentText.text = _currentNode.Text;
                            _typeFinished = true;
                            AfterTypeFinished_ShowChoicesOrContinue();
                        }
                        else
                        {
                            // 已显示选项，鼠标点击立即触发对应选项事件
                            // 找到被点击按钮的索引（保证与键盘路径一致）
                            int clickedIndex = idx;
                            OnChoiceClicked?.Invoke(clickedIndex);
                        }
                    });
                }
            }
            else
            {
                // 没有选项时，创建一个 Continue 按钮（同样先隐藏）
                var btn = Instantiate(choiceButtonPrefab, choicesContainer);
                btn.onClick.RemoveAllListeners();
                btn.gameObject.SetActive(false);
                _choiceButtons.Add(btn);

                var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = "Continue";

                btn.onClick.AddListener(() =>
                {
                    if (_typeRoutine != null)
                    {
                        StopCoroutine(_typeRoutine);
                        _typeRoutine = null;
                        if (_currentNode != null && contentText != null) contentText.text = _currentNode.Text;
                        _typeFinished = true;
                        AfterTypeFinished_ShowChoicesOrContinue();
                    }
                    else
                    {
                        // 单一 Continue 按钮被按下，等同于继续
                        _awaitingContinue = false;
                        OnContinueClicked?.Invoke();
                    }
                });
            }
        }
    }

    private void OnDialogueEnded()
    {
        if (_typeRoutine != null)
        {
            StopCoroutine(_typeRoutine);
            _typeRoutine = null;
        }
        _typeFinished = false;
        _awaitingContinue = false;
        _currentNode = null;

        // 清理选项
        foreach (Transform t in choicesContainer) Destroy(t.gameObject);
        _choiceButtons.Clear();
        _selectedIndex = -1;

        if (root != null) root.SetActive(false);
    }

    private IEnumerator TypeTextRoutine(string text)
    {
        if (contentText == null) yield break;
        contentText.text = "";
        if (string.IsNullOrEmpty(text))
        {
            _typeRoutine = null;
            _typeFinished = true;
            // 如果没有选项，进入等待继续（但此处仍在文字结束后处理）
            if (_currentNode != null && (_currentNode.Choices == null || _currentNode.Choices.Count == 0))
                _awaitingContinue = true;
            // 显示预创建的 Continue / 选项
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

        // 打字结束后显示选项或进入等待继续（由 AfterTypeFinished_ShowChoicesOrContinue 处理）
        AfterTypeFinished_ShowChoicesOrContinue();
    }

    // 当文字完全显示后调用：显示预先创建的按钮并设置初始选中状态或进入等待继续
    private void AfterTypeFinished_ShowChoicesOrContinue()
    {
        // 如果没有创建按钮（理论上不会发生），设置等待继续
        if (_choiceButtons.Count == 0)
        {
            _awaitingContinue = true;
            return;
        }

        // 激活所有按钮（显示在 UI 中）
        for (int i = 0; i < _choiceButtons.Count; i++)
        {
            var b = _choiceButtons[i];
            if (b != null) b.gameObject.SetActive(true);
        }

        // 若有多个选项，默认选择第一个；若只有一个 Continue，也默认选中
        _selectedIndex = 0;
        SelectButtonAt(_selectedIndex);

        // 对于没有实际选项的 Continue 场景，标记等待继续以允许外部确认（例如按键确认）
        if (_currentNode != null && (_currentNode.Choices == null || _currentNode.Choices.Count == 0))
        {
            _awaitingContinue = true;
        }
        else
        {
            _awaitingContinue = false;
        }
    }

    private void SetSelected(int idx)
    {
        if (_choiceButtons.Count == 0) return;
        idx = Mathf.Clamp(idx, 0, _choiceButtons.Count - 1);
        _selectedIndex = idx;
        SelectButtonAt(_selectedIndex);
    }

    private void SelectButtonAt(int idx)
    {
        if (_choiceButtons.Count == 0) return;
        var btn = _choiceButtons[Mathf.Clamp(idx, 0, _choiceButtons.Count - 1)];
        if (btn != null)
        {
            // 使用 EventSystem 来高亮/选中按钮，以便于键盘导航可见
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(btn.gameObject);
        }
    }
}