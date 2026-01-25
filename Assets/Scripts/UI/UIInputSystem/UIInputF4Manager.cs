using UnityEngine;
using VContainer;

// 负责 F4 键的处理（拆分自 UIInputManager）
public class UIInputF4Manager : MonoBehaviour
{
    private UIManager _uiManager;
    private IGlobalEventVariables _globalEventVariables;

    [Inject]
    public void Inject(UIManager uiManager, IGlobalEventVariables globalEventVariables)
    {
        _uiManager = uiManager;
        _globalEventVariables = globalEventVariables;
    }

    // F4 隐藏/恢复相关
    private bool _hideAllActive = false;

    private void Update()
    {
        // F4：隐藏全部并记住当前可见列表；再次按恢复（仅恢复先前可见的）
        if (Input.GetKeyDown(KeyCode.F4))
        {
            // toggle hide-all
            if (!_hideAllActive && _globalEventVariables.GetEnum<UIState>(GlobalEventKey.UIState) == UIState.Main)
            {
                _uiManager.HideAndRecordVisible();
                _hideAllActive = true;
                _globalEventVariables.SetEnum(GlobalEventKey.UIState, UIState.Hidden);
            }
            else
            {
                _uiManager.ShowRecordedVisible();
                _hideAllActive = false;
                _globalEventVariables.SetEnum(GlobalEventKey.UIState, UIState.Main);
            }
        }
    }
}
