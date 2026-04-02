using UnityEngine;
using VContainer;

/// <summary>
/// 简单的输入路由器：把键盘输入转发给 DialogueUI
/// - ↑/W/Main/A 向上选择，↓/S/Right/D 向下选择（左右也映射为上下）
/// - Enter / Return 或 Space 确认
/// - 当文字正在打字时，按 Enter/Space 或 鼠标左键 直接显示全部文本
/// - 仅当 DialogueManager.IsActive 时才处理对话输入
/// - 请把此组件放在场景中（如挂在一个 GameManager 对象）并确保存在 EventSystem
/// </summary>
public class DialogueInputManager : MonoBehaviour
{
    private UIDialogue _uiDialogue;
    private DialogueManager _dialogueManager;
    private IGlobalEventVariables _globalEventVariables;

    [Inject]
    public void Construct(DialogueManager dialogueManager, IGlobalEventVariables globalEventVariables, UIDialogue uiDialogue)
    {
        _dialogueManager = dialogueManager;
        _globalEventVariables = globalEventVariables;
        _uiDialogue = uiDialogue;
    }
    private void Update()
    {
        if (_dialogueManager == null || !_globalEventVariables.GetBool(GlobalEventKey.DialogueIsActive) || _uiDialogue == null) return;

        // 先处理：如果当前正在打字（文字未显示完全），按键或鼠标点击应直接显示全部文本
        if (!_uiDialogue.IsTypeFinished())
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)
                || Input.GetMouseButtonDown(0))
            {
                // 优先调用公开 Confirm()（如果存在），否则通过反射停止协程并展示全部文本
                _uiDialogue.Confirm();
                return;
            }
        }

        // 导航（仅当选项已可见时）
        if (_uiDialogue.IsChoicesVisible())
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                _uiDialogue.Navigate(-1);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                _uiDialogue.Navigate(1);
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            {
                _uiDialogue.Confirm();
            }
        }
        else
        {
            // 当没有可见选项但处于等待继续时，允许 Enter/Space 触发继续
            if (_uiDialogue.IsAwaitingContinue() && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)))
            {
                _uiDialogue.Confirm();
            }
        }
    }
}