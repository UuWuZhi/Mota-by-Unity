using UnityEngine;
using VContainer;

// 负责 F4 键的处理（拆分自 UIInputManager）
public class UIInputF4Manager : MonoBehaviour
{
    private IGlobalEventVariables _globalEventVariables;
    private UIManager _uiManager;

    [Inject]
    public void Inject(UIManager uiManager, IGlobalEventVariables globalEventVariables)
    {
        _uiManager = uiManager;
        _globalEventVariables = globalEventVariables;
    }

    private void Update()
    {
        // F4：隐藏全部并记住当前可见列表；再次按恢复（仅恢复先前可见的）
        if (Input.GetKeyDown(KeyCode.F4))
        {
            if (_uiManager == null) return;
            if (_globalEventVariables.GetEnum<UIState>(GlobalEventKey.UIState) == UIState.Main)
            {
                // 直接隐藏并记录当前可见 UI
                _uiManager.HideAndRecordVisible();
                _globalEventVariables.SetEnum(GlobalEventKey.UIState, UIState.Hidden);
            }
            else if (_globalEventVariables.GetEnum<UIState>(GlobalEventKey.UIState) == UIState.Hidden)
            {
                // 直接显示先前记录的 UI
                _uiManager.ShowRecordedVisible();
                _globalEventVariables.SetEnum(GlobalEventKey.UIState, UIState.Main);
            }
        }
    }
}
