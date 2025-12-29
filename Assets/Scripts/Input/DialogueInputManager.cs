using UnityEngine;
using VContainer;

/// <summary>
/// 简单的输入路由器：把键盘输入转发给 DialogueUI
/// - ↑/W/Left/A 向上选择，↓/S/Right/D 向下选择（左右也映射为上下）
/// - Enter / Return 或 Space 确认
/// - 当文字正在打字时，按 Enter/Space 或 鼠标左键 直接显示全部文本
/// - 仅当 DialogueManager.IsActive 时才处理对话输入
/// - 请把此组件放在场景中（如挂在一个 GameManager 对象）并确保存在 EventSystem
/// </summary>
public class DialogueInputManager : MonoBehaviour
{
    private UIDialogue _ui;
    private DialogueManager _dm;

    [Inject]
    public void Inject(DialogueManager dialogueManager)
    {
        _dm = dialogueManager;
    }

    private void Start()
    {
        _ui = GameObject.FindObjectOfType<UIDialogue>();
        CacheReflection();
    }

    private void CacheReflection()
    {
        if (_ui == null) return;
        var t = _ui.GetType();
    }

    private void Update()
    {
        if (_ui == null) _ui = GameObject.FindObjectOfType<UIDialogue>();

        if (_ui != null)
        {
            // 可能刚刚创建或加载了新的 UI，重新缓存反射信息
            CacheReflection();
        }

        if (_dm == null || !_dm.IsActive || _ui == null) return;

        // 先处理：如果当前正在打字（文字未显示完全），按键或鼠标点击应直接显示全部文本
        if (IsTyping())
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)
                || Input.GetMouseButtonDown(0))
            {
                // 优先调用公开 Confirm()（如果存在），否则通过反射停止协程并展示全部文本
                _ui.Confirm();
                return;
            }
        }

        // 导航（仅当选项已可见时）
        if (_ui.ChoicesVisible)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                _ui.Navigate(-1);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                _ui.Navigate(1);
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            {
                _ui.Confirm();
            }
        }
        else
        {
            // 当没有可见选项但处于等待继续时，允许 Enter/Space 触发继续
            if (_ui.IsAwaitingContinue && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)))
            {
                _ui.Confirm();
            }
        }
    }

    // 判断 DialogueUI 是否正在打字（兼容有 Confirm() 或使用私有字段的旧实现）
    private bool IsTyping()
    {
        if (_ui == null) return false;
        return !_ui.TypeFinished;
    }
}