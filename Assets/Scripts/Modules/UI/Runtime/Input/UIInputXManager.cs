using System.Collections.Generic;
using Modules.Core.DataDefine;
using Modules.UI.DataDefine;
using UnityEngine;
using VContainer;

// 负责 X 键的处理（拆分自 UIInputManager）
namespace Modules.UI.Runtime.Input
{
    public class UIInputXManager : MonoBehaviour
    {
        // X 键模式（第一次按 X 会隐藏当前全部并显示指定面板，之后按 X 切换这些面板的显示/隐藏）
        private readonly List<UIRootType> _xTargetTypes = new() { UIRootType.MonsterBook, UIRootType.SideMenu };
        private IGlobalEventVariables _globalEventVariables;
        private UIManager _uiManager;


        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.X)) HandleXPress();
        }

        [Inject]
        public void Inject(UIManager uiManager, IGlobalEventVariables globalEventVariables)
        {
            _uiManager = uiManager;
            _globalEventVariables = globalEventVariables;
        }

        /// <summary>
        ///     对外方法：可以由 UI Button 的 OnClick 调用，执行与按下 X 相同的逻辑
        /// </summary>
        public void TriggerX()
        {
            HandleXPress();
        }

        // 抽取 X 键的处理逻辑，供 Update 与 UI 按钮复用
        private void HandleXPress()
        {
            if (!_uiManager) return;
            if (_globalEventVariables.GetEnum<UIState>(GlobalEventKey.UIState) == UIState.Main)
            {
                _uiManager.HideUI(new List<UIRootType> { UIRootType.Main });
                _uiManager.ShowUI(_xTargetTypes);
                _globalEventVariables.SetEnum(GlobalEventKey.UIState, UIState.Menu);
            }
            else if (_globalEventVariables.GetEnum<UIState>(GlobalEventKey.UIState) == UIState.Menu)
            {
                _uiManager.HideUI(_xTargetTypes);
                _uiManager.ShowUI(new List<UIRootType> { UIRootType.Main });
                _globalEventVariables.SetEnum(GlobalEventKey.UIState, UIState.Main);
            }
        }
    }
}