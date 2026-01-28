using UnityEngine;
using System.Collections.Generic;
using VContainer;

// 负责 X 键的处理（拆分自 UIInputManager）
public class UIInputXManager : MonoBehaviour
{
    private EventCenter _eventCenter;
    private IGlobalEventVariables _globalEventVariables;

    [Inject]
    public void Inject(EventCenter eventCenter, IGlobalEventVariables globalEventVariables)
    {
        _eventCenter = eventCenter;
        _globalEventVariables = globalEventVariables;
    }

    // X 键模式（第一次按 X 会隐藏当前全部并显示指定面板，之后按 X 切换这些面板的显示/隐藏）
    private readonly List<UIRootType> _xTargetTypes = new List<UIRootType>() { UIRootType.MonsterBook, UIRootType.SideMenu };


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            HandleXPress();
        }
    }

    /// <summary>
    /// 对外方法：可以由 UI Button 的 OnClick 调用，执行与按下 X 相同的逻辑
    /// </summary>
    public void TriggerX()
    {
        HandleXPress();
    }

    // 抽取 X 键的处理逻辑，供 Update 与 UI 按钮复用
    private void HandleXPress()
    {
        if (_globalEventVariables.GetEnum<UIState>(GlobalEventKey.UIState) == UIState.Main)
        {
            _eventCenter.TriggerHideUI(new UIHideEventArgs { UITypes = new List<UIRootType> { UIRootType.Main } });
            _eventCenter.TriggerShowUI(new UIShowEventArgs { UITypes = _xTargetTypes });
            _globalEventVariables.SetEnum(GlobalEventKey.UIState, UIState.Menu);
        }
        else if (_globalEventVariables.GetEnum<UIState>(GlobalEventKey.UIState) == UIState.Menu)
        {
            _eventCenter.TriggerHideUI(new UIHideEventArgs { UITypes = _xTargetTypes });
            _eventCenter.TriggerShowUI(new UIShowEventArgs { UITypes = new List<UIRootType> { UIRootType.Main } });
            _globalEventVariables.SetEnum(GlobalEventKey.UIState, UIState.Main);
        }
    }
}
