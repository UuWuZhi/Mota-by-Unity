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
public class UIDialogue : MonoBehaviour
{
    [Header("UI 元素")]
    [Tooltip("对话根节点（用于整体显示/隐藏)")]
    public GameObject root;
    [Tooltip("说话者文本")]
    public TextMeshProUGUI speakerText;
    [Tooltip("对话内容 TextMeshProUGUI")]
    public TextMeshProUGUI contentText;
    [Tooltip("选项容器（直接把按钮放在这里)")]
    public Transform choicesContainer;
    [Tooltip("选项按钮预制（Button + TextMeshProUGUI)")]
    public Button choiceButtonPrefab;

    [Header("打字机参数")]
    public float typeSpeed = 0.02f;

    private Coroutine _typeRoutine; // 打字协程引用
    private DialogueData _currentDialogue;

    private bool _typeFinished = false;       // 文字是否已经全部显示
    private bool _eventSubscribed = false;
    private bool _registeredRoot = false;
    private bool _awaitingContinue = false;   // 是否处于等待继续（无选项时）
    public bool ChoicesVisible => _choiceButtons.Count > 0 && _choiceButtons[0].gameObject.activeSelf;
    public bool IsAwaitingContinue => _awaitingContinue;
    public bool TypeFinished => _typeFinished;

    private readonly List<Button> _choiceButtons = new List<Button>();
    private int _selectedIndex = -1;

    private DialogueManager _dialogueManager;
    private UIManager _uiManager;
    //==============================================================================//
    //                                                                              //
    //                                 生命周期                                     //
    //                                                                              //
    //==============================================================================//
    #region 生命周期
    private void Start()
    {
        if (_dialogueManager == null)
        {
            Debug.LogWarning("DialogueUI: 未找到 DialogueManager，UI 将不会工作。");
        }
        else
        {
            SubscribeDialogueManager();
        }

        TryRegisterRoot();

        if (root != null) root.SetActive(false);
    }

    private void OnDestroy()
    {
        UnsubscribeDialogueManager();
    }

    [Inject]
    public void Inject(DialogueManager dialogueManager, UIManager uiManager)
    {
        _dialogueManager = dialogueManager;
        _uiManager = uiManager;
        SubscribeDialogueManager();
        TryRegisterRoot();
    }
    #endregion
    //==============================================================================//
    //                                                                              //
    //                                 事件系统                                     //
    //                                                                              //
    //==============================================================================//
    #region 事件系统
    private void OnDialogueStarted()
    {
        if (root != null) root.SetActive(true);
    }
    private void OnDialogueChanged(DialogueData data)
    {
        if (data == null) return;
        _currentDialogue = data;

        if (speakerText != null)
            speakerText.text = string.IsNullOrEmpty(data.Speaker) ? "" : data.Speaker;

        _typeFinished = false;
        _awaitingContinue = false;

        if (_typeRoutine != null) StopCoroutine(_typeRoutine);
        _typeRoutine = StartCoroutine(TypeTextRoutine(data.Text));

        foreach (Transform t in choicesContainer) Destroy(t.gameObject);
        _choiceButtons.Clear();
        _selectedIndex = -1;

        if (choicesContainer != null && choiceButtonPrefab != null)
        {
            if (data.Options != null && data.Options.Count > 0)
            {
                for (int i = 0; i < data.Options.Count; i++)
                {
                    int idx = i;
                    var btn = Instantiate(choiceButtonPrefab, choicesContainer);
                    btn.onClick.RemoveAllListeners();
                    btn.gameObject.SetActive(false);
                    _choiceButtons.Add(btn);

                    var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmp != null) tmp.text = data.Options[i].Text;
                    else Debug.LogWarning("DialogueUI: choiceButtonPrefab 缺少 TextMeshProUGUI");

                    btn.onClick.AddListener(() =>
                    {
                        if (_typeRoutine != null)
                        {
                            StopCoroutine(_typeRoutine);
                            _typeRoutine = null;
                            if (_currentDialogue != null && contentText != null) contentText.text = _currentDialogue.Text;
                            _typeFinished = true;
                            AfterTypeFinished_ShowChoicesOrContinue();
                        }
                        else
                        {
                            int clickedIndex = idx;
                            if (_dialogueManager != null)
                            {
                                _dialogueManager.NotifyUIChoiceSelected(clickedIndex);
                            }
                            //OnChoiceClicked?.Invoke(clickedIndex);
                        }
                    });
                }
            }
            else
            {
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
                        if (_currentDialogue != null && contentText != null) contentText.text = _currentDialogue.Text;
                        _typeFinished = true;
                        AfterTypeFinished_ShowChoicesOrContinue();
                    }
                    else
                    {
                        _awaitingContinue = false;
                        if (_dialogueManager != null)
                        {
                            _dialogueManager.NotifyUIContinue();
                        }
                        //OnContinueClicked?.Invoke();
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
        _currentDialogue = null;

        foreach (Transform t in choicesContainer) Destroy(t.gameObject);
        _choiceButtons.Clear();
        _selectedIndex = -1;

        if (root != null) root.SetActive(false);
    }
    private void TryRegisterRoot()
    {
        if (_registeredRoot) return;
        if (_uiManager == null || root == null) return;
        _uiManager.RegisterUIRoot(root);
        _registeredRoot = true;
    }
    private void SubscribeDialogueManager()
    {
        if (_dialogueManager == null || _eventSubscribed) return;
        _dialogueManager.OnDialogueStarted += OnDialogueStarted;
        _dialogueManager.OnDialogueChanged += OnDialogueChanged;
        _dialogueManager.OnDialogueEnded += OnDialogueEnded;
        _eventSubscribed = true;
    }
    private void UnsubscribeDialogueManager()
    {
        if (_dialogueManager == null || !_eventSubscribed) return;
        _dialogueManager.OnDialogueStarted -= OnDialogueStarted;
        _dialogueManager.OnDialogueChanged -= OnDialogueChanged;
        _dialogueManager.OnDialogueEnded -= OnDialogueEnded;
        _eventSubscribed = false;
    }
    #endregion

    public void Navigate(int direction)
    {
        if (!ChoicesVisible || _choiceButtons.Count == 0) return;
        int count = _choiceButtons.Count;
        if (count == 0) return;
        int next = (_selectedIndex + direction + count) % count;
        SetSelected(next);
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
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(btn.gameObject);
        }
    }

    public void Confirm()
    {
        if (_typeRoutine != null)
        {
            StopCoroutine(_typeRoutine);
            _typeRoutine = null;
            if (_currentDialogue != null && contentText != null) contentText.text = _currentDialogue.Text;
            _typeFinished = true;

            AfterTypeFinished_ShowChoicesOrContinue();
            return;
        }

        if (ChoicesVisible && _choiceButtons.Count > 0)
        {
            int idx = Mathf.Clamp(_selectedIndex, 0, _choiceButtons.Count - 1);
            // 通过 DialogueManager 回调外部系统
            if (_dialogueManager != null)
            {
                _dialogueManager.NotifyUIChoiceSelected(idx);
            }
            //OnChoiceClicked?.Invoke(idx);
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
            //OnContinueClicked?.Invoke();
        }
    }

    private IEnumerator TypeTextRoutine(string text)
    {
        if (contentText == null) yield break;
        contentText.text = "";
        if (string.IsNullOrEmpty(text))
        {
            _typeRoutine = null;
            _typeFinished = true;
            if (_currentDialogue != null && (_currentDialogue.Options == null || _currentDialogue.Options.Count == 0))
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

    private void AfterTypeFinished_ShowChoicesOrContinue()
    {
        if (_choiceButtons.Count == 0)
        {
            _awaitingContinue = true;
            return;
        }

        for (int i = 0; i < _choiceButtons.Count; i++)
        {
            var b = _choiceButtons[i];
            if (b != null) b.gameObject.SetActive(true);
        }

        _selectedIndex = 0;
        SelectButtonAt(_selectedIndex);

        if (_currentDialogue != null && (_currentDialogue.Options == null || _currentDialogue.Options.Count == 0))
        {
            _awaitingContinue = true;
        }
        else
        {
            _awaitingContinue = false;
        }
    }
}