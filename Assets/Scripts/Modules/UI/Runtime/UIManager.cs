using System.Collections.Generic;
using System.Linq;
using Modules.UI.DataDefine;
using UnityEngine;
using UnityEngine.Serialization;

// 中央 UI 管理器：负责注册场景中所有可控制的 UI 根对象，并统一显示/隐藏
namespace Modules.UI.Runtime
{
    public class UIManager : MonoBehaviour
    {
        [FormerlySerializedAs("RecordedUITypes")]
        public List<UIRootType> recordedUITypes = new();

        // 枚举类型映射（由数据库或 RegisterUIRoot 自动维护）
        // 改为持有 BaseUI，便于通过 Show/Hide/Toggle 调用而不是直接 SetActive
        private readonly Dictionary<UIRootType, BaseUI> _rootsByType = new();

        #region 节点管理

        // 显式注册并指定类型（推荐用于运行时动态创建的 UI）
        // 现在 Register/Unregister 接受 BaseUI 实例
        public void RegisterUIRoot(BaseUI ui, UIRootType type)
        {
            if (!ui || type == UIRootType.None) return;
            _rootsByType[type] = ui;
        }

        public void UnregisterUIRoot(BaseUI ui)
        {
            if (!ui) return;
            // 清理枚举映射中引用相同的项
            var keysToRemove = (from kv in _rootsByType where kv.Value == ui select kv.Key).ToList();
            foreach (var k in keysToRemove) _rootsByType.Remove(k);
        }
        // 新增：提供对外读取 UI 根及其显示状态的接口
        // string-based name list removed; use GetActiveRootTypes() instead

        // 新：以枚举返回当前激活的 UI 列表
        public List<UIRootType> GetActiveRootTypes()
        {
            return (from kv in _rootsByType let baseUI = kv.Value where baseUI where baseUI.IsVisible select kv.Key)
                .ToList();
        }

        // string-based lookup removed

        public bool IsUIRootRegistered(UIRootType type)
        {
            if (type == UIRootType.None) return false;
            return _rootsByType.ContainsKey(type) && _rootsByType[type];
        }

        public bool IsUIRootActive(UIRootType type)
        {
            if (type == UIRootType.None) return false;
            if (_rootsByType.TryGetValue(type, out var ui) && ui) return ui.IsVisible;
            return false;
        }

        #endregion

        #region 显示方法

        // string-based ShowUI removed; use enum-based ShowUI

        // 枚举版本 ShowUI
        public void ShowUI(List<UIRootType> types)
        {
            if (types == null || types.Count == 0)
            {
                foreach (var kv in _rootsByType.Where(kv => kv.Value))
                    kv.Value.Show();
                return;
            }

            foreach (var t in types.Where(t => t != UIRootType.None))
                if (_rootsByType.TryGetValue(t, out var ui) && ui)
                    ui.Show();
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
                foreach (var kv in _rootsByType.Where(kv => kv.Value))
                    kv.Value.Hide();
                return;
            }

            foreach (var t in types.Where(t => t != UIRootType.None))
                if (_rootsByType.TryGetValue(t, out var ui) && ui)
                    ui.Hide();
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
                foreach (var kv in _rootsByType.Where(kv => kv.Value)) kv.Value.Toggle();

                return;
            }

            foreach (var t in types.Where(t => t != UIRootType.None))
                if (_rootsByType.TryGetValue(t, out var ui) && ui)
                    ui.Toggle();
        }

        // 新增：封装 “隐藏并记录当前可见 UI” 与 “显示记录的 UI” 的方法
        // HideAndRecordVisible: 返回被隐藏的 UI 名称列表（可能为空）
        public List<UIRootType> HideAndRecordVisible()
        {
            var active = GetActiveRootTypes();
            if (active is { Count: > 0 }) HideUI(active);
            recordedUITypes = active;
            return active ?? new List<UIRootType>();
        }

        // 枚举版本 HideAndRecordVisible
        public List<UIRootType> HideAndRecordVisibleByType()
        {
            var active = GetActiveRootTypes();
            if (active is { Count: > 0 }) HideUI(active);
            return active ?? new List<UIRootType>();
        }

        // ShowRecordedVisible: 传入先前记录的名称列表，尝试恢复显示其中注册过的 UI 根
        public void ShowRecordedVisible(List<UIRootType> recordedTypes)
        {
            if (recordedTypes == null || recordedTypes.Count == 0) return;
            var restore = recordedTypes.Where(t => t != UIRootType.None).Where(IsUIRootRegistered).ToList();

            if (restore.Count > 0) ShowUI(restore);
        }

        public void ShowRecordedVisible()
        {
            ShowRecordedVisible(recordedUITypes);
        }

        #endregion
    }
}