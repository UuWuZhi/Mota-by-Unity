using System.Collections.Generic;
using UnityEngine;
using VContainer;

/// <summary>
/// 简易测试脚本：按 E 切换背包面板的显示（用于测试期）
/// 不考虑其他 UI 状态，直接切换 InventoryUI.root 的 active 状态。
/// 将此脚本挂到场景中任意 GameObject，或者在 Inspector 手动指定 targetInventoryUI。
/// </summary>
public class InventoryUIInput : MonoBehaviour
{
    private UIManager _uiManager;
    private IGlobalEventVariables _globalEventVariables;

    [Inject]
    public void Inject(UIManager uiManager, IGlobalEventVariables globalEventVariables)
    {
        _uiManager = uiManager;
        _globalEventVariables = globalEventVariables;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            ToggleInventory();
        }
    }

    private void ToggleInventory()
    {
        if (_uiManager == null) return;
        if (_globalEventVariables.GetEnum<UIState>(GlobalEventKey.UIState) == UIState.Main)
        {
            _uiManager.ToggleUI(new List<UIRootType> { UIRootType.Inventory });
        }
    }
}
