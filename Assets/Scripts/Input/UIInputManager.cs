using UnityEngine;
using System.Collections.Generic;
using VContainer;

// 负责识别与 UI 相关的按键（例如 F4 隐藏所有 UI）并通过 EventCenter 发布事件
public class UIInputManager : MonoBehaviour
{
    // 注入（可选）
    private UIManager _uiManager;
    private EventCenter _eventCenter;
    private IGlobalEventVariables _globalEventVariables;

    [Inject]
    public void Inject(UIManager uiManager, EventCenter eventCenter, IGlobalEventVariables globalEventVariables)
    {
        _uiManager = uiManager;
        _eventCenter = eventCenter;
        _globalEventVariables = globalEventVariables;
    }

    // 记录启动时哪些 UI 根是可见的（用于遵守“一开始就隐藏的不影响”）
    private HashSet<string> _initiallyVisible = new HashSet<string>();

    // F4 隐藏/恢复相关
    private bool _hideAllActive = false;
    private List<string> _savedVisibleBeforeHideAll = new List<string>();

    // X 键模式（第一次按 X 会隐藏当前全部并显示指定面板，之后按 X 切换这些面板的显示/隐藏）
    private bool _xModeActive = false;
    private readonly string[] _xTargetNames = new[] { "MonsterBook", "SideMenu" };

    private void Awake()
    {
        // 记录场景中一开始哪些 UI 是激活的（若 UIManager 可用）
        var uiMgr = _uiManager ?? UIManager.Instance;
        if (uiMgr != null)
        {
            var active = uiMgr.GetActiveRootNames();
            foreach (var n in active) if (!string.IsNullOrEmpty(n)) _initiallyVisible.Add(n);
        }
    }

    private void Update()
    {
        var uiMgr = _uiManager ?? UIManager.Instance;
        var ec = _eventCenter ?? EventCenter.Instance;

        // F4：隐藏全部并记住当前可见列表；再次按恢复（仅恢复先前可见的）
        if (Input.GetKeyDown(KeyCode.F4))
        {
            // toggle hide-all
            if (!_hideAllActive)
            {
                uiMgr.HideAndRecordVisible();
                _hideAllActive = true;
                _globalEventVariables.SetString(GlobalEventKey.UIState, "HiddenAll");
            }
            else
            {
                uiMgr.ShowRecordedVisible();
                _hideAllActive = false;
                _globalEventVariables.SetString(GlobalEventKey.UIState, "InGame");
            }
        }

        // X：只有当未处于 HideAll 时才生效
        if (Input.GetKeyDown(KeyCode.X))
        {
            HandleXPress();
        }

        // 未来可加入 Esc/Tab 等按键来切换背包/地图等
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
        if (_hideAllActive) return; // 忽略

        var uiMgr = _uiManager;
        var ec = _eventCenter;

        // 第一次按 X：隐藏目前可见的全部 UI，然后显示 MonsterBook 与 SideMenu（仅当它们一开始不是隐藏的）
        if (!_xModeActive)
        {
            uiMgr?.HideAndRecordVisible();
            var toShow = new List<string>();
            foreach (var n in _xTargetNames)
            {
                if (!string.IsNullOrEmpty(n) && uiMgr != null && uiMgr.IsUIRootRegistered(n))
                    toShow.Add(n);
            }
            if (toShow.Count > 0)
                ec?.TriggerShowUI(new UIShowEventArgs { UINames = toShow });

            _xModeActive = true;
        }
        else
        {
            // 之后按 X：切换 MonsterBook 与 SideMenu 的显示状态（仅针对那些一开始不是隐藏的）
            uiMgr?.ShowRecordedVisible();
            var toHide = new List<string>();
            foreach (var n in _xTargetNames)
            {
                if (!string.IsNullOrEmpty(n) && uiMgr != null && uiMgr.IsUIRootRegistered(n))
                    toHide.Add(n);
            }
            if (toHide.Count > 0)
            {
                ec?.TriggerHideUI(new UIHideEventArgs { UINames = toHide });
            }

            _xModeActive = false;
        }
    }
}
