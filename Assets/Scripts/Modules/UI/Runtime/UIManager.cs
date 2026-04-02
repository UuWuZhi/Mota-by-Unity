using System.Collections.Generic;
using UnityEngine;

// 中央 UI 管理器：负责注册场景中所有可控制的 UI 根对象，并统一显示/隐藏
public class UIManager : MonoBehaviour
{
    public List<UIRootType> RecordedUITypes = new List<UIRootType>();

    // 枚举类型映射（由数据库或 RegisterUIRoot 自动维护）
    // 改为持有 BaseUI，便于通过 Show/Hide/Toggle 调用而不是直接 SetActive
    private readonly Dictionary<UIRootType, BaseUI> _rootsByType = new Dictionary<UIRootType, BaseUI>();


    //==============================================================================//
    //                                                                              //
    //                                 生命周期                                     //
    //                                                                              //
    //==============================================================================//
    #region 生命周期

    #endregion
    //==============================================================================//
    //                                                                              //
    //                                 节点管理                                     //
    //                                                                              //
    //==============================================================================//
    #region 节点管理
    // 显式注册并指定类型（推荐用于运行时动态创建的 UI）
    // 现在 Register/Unregister 接受 BaseUI 实例
    public void RegisterUIRoot(BaseUI ui, UIRootType type)
    {
        if (ui == null || type == UIRootType.None) return;
        _rootsByType[type] = ui;
    }

    public void UnregisterUIRoot(BaseUI ui)
    {
        if (ui == null) return;
        // 清理枚举映射中引用相同的项
        var keysToRemove = new List<UIRootType>();
        foreach (var kv in _rootsByType)
        {
            if (kv.Value == ui) keysToRemove.Add(kv.Key);
        }
        foreach (var k in keysToRemove) _rootsByType.Remove(k);
    }
    // 新增：提供对外读取 UI 根及其显示状态的接口
    // string-based name list removed; use GetActiveRootTypes() instead

    // 新：以枚举返回当前激活的 UI 列表
    public List<UIRootType> GetActiveRootTypes()
    {
        var result = new List<UIRootType>();
        foreach (var kv in _rootsByType)
        {
            var baseUI = kv.Value;
            if (baseUI == null) continue;
            if (baseUI.IsVisible) result.Add(kv.Key);
        }
        return result;
    }

    // string-based lookup removed

    public bool IsUIRootRegistered(UIRootType type)
    {
        if (type == UIRootType.None) return false;
        return _rootsByType.ContainsKey(type) && _rootsByType[type] != null;
    }

    public bool IsUIRootActive(UIRootType type)
    {
        if (type == UIRootType.None) return false;
        if (_rootsByType.TryGetValue(type, out var ui) && ui != null) return ui.IsVisible;
        return false;
    }
    #endregion
    //==============================================================================//
    //                                                                              //
    //                                 显示方法                                     //
    //                                                                              //
    //==============================================================================//
    #region 显示方法
    // string-based ShowUI removed; use enum-based ShowUI

    // 枚举版本 ShowUI
    public void ShowUI(List<UIRootType> types)
    {
        if (types == null || types.Count == 0)
        {
            foreach (var kv in _rootsByType) if (kv.Value != null) kv.Value.Show();
            return;
        }
        foreach (var t in types)
        {
            if (t == UIRootType.None) continue;
            if (_rootsByType.TryGetValue(t, out var ui) && ui != null) ui.Show();
        }
    }

    public void ShowUI(UIRootType type)
    {
        ShowUI(new List<UIRootType> { type });
    }

    // string-based HideUI removed; use enum-based HideUI

    // 枚举版本 HideUI
    public void HideUI(List<UIRootType> types)
    {
        if (types == null || types.Count == 0)
        {
            foreach (var kv in _rootsByType) if (kv.Value != null) kv.Value.Hide();
            return;
        }
        foreach (var t in types)
        {
            if (t == UIRootType.None) continue;
            if (_rootsByType.TryGetValue(t, out var ui) && ui != null) ui.Hide();
        }
    }

    public void HideUI(UIRootType type)
    {
        HideUI(new List<UIRootType> { type });
    }

    // string-based ToggleUI removed; use enum-based ToggleUI

    // 枚举版本 Toggle
    public void ToggleUI(List<UIRootType> types)
    {
        if (types == null || types.Count == 0)
        {
            foreach (var kv in _rootsByType)
            {
                if (kv.Value == null) continue;
                kv.Value.Toggle();
            }
            return;
        }
        foreach (var t in types)
        {
            if (t == UIRootType.None) continue;
            if (_rootsByType.TryGetValue(t, out var ui) && ui != null) ui.Toggle();
        }
    }
    // 新增：封装 “隐藏并记录当前可见 UI” 与 “显示记录的 UI” 的方法
    // HideAndRecordVisible: 返回被隐藏的 UI 名称列表（可能为空）
    public List<UIRootType> HideAndRecordVisible()
    {
        var active = GetActiveRootTypes();
        if (active != null && active.Count > 0)
        {
            HideUI(active);
        }
        RecordedUITypes = active;
        return active ?? new List<UIRootType>();
    }

    // 枚举版本 HideAndRecordVisible
    public List<UIRootType> HideAndRecordVisibleByType()
    {
        var active = GetActiveRootTypes();
        if (active != null && active.Count > 0)
        {
            HideUI(active);
        }
        return active ?? new List<UIRootType>();
    }

    // ShowRecordedVisible: 传入先前记录的名称列表，尝试恢复显示其中注册过的 UI 根
    public void ShowRecordedVisible(List<UIRootType> recordedTypes)
    {
        if (recordedTypes == null || recordedTypes.Count == 0) return;
        var restore = new List<UIRootType>();
        foreach (var t in recordedTypes)
        {
            if (t == UIRootType.None) continue;
            if (IsUIRootRegistered(t)) restore.Add(t);
        }
        if (restore.Count > 0) ShowUI(restore);
    }
    public void ShowRecordedVisible()
    {
        ShowRecordedVisible(RecordedUITypes);
    }
    #endregion
}