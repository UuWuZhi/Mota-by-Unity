using System.Collections.Generic;
using UnityEngine;
using VContainer; // for [Inject]

// 中央 UI 管理器：负责注册场景中所有可控制的 UI 根对象，并统一显示/隐藏
public class UIManager : MonoBehaviour
{
    public List<UIRootType> RecordedUITypes = new List<UIRootType>();
    [SerializeField] private UIRootDatabase _uiRootDatabase;

    // 枚举类型映射（由数据库或 RegisterUIRoot 自动维护）
    private readonly Dictionary<UIRootType, GameObject> _rootsByType = new Dictionary<UIRootType, GameObject>();

    // injected EventCenter (via VContainer)
    private EventCenter _eventCenter;
    private bool _subscribedToEvents = false;

    //==============================================================================//
    //                                                                              //
    //                                 生命周期                                     //
    //                                                                              //
    //==============================================================================//
    #region 生命周期

    private void OnEnable()
    {
        // Try to subscribe; if DI hasn't injected EventCenter yet, Inject(...) will call TrySubscribe later
        TrySubscribeEvents();
    }

    private void Awake()
    {
        // Ensure database-driven mapping exists early (before IStartable runs)
        InitializeFromDatabase();
    }

    private void OnDisable()
    {
        TryUnsubscribeEvents();
    }

    [Inject]
    public void Inject(EventCenter eventCenter)
    {
        _eventCenter = eventCenter;
        TrySubscribeEvents();
        InitializeFromDatabase();
    }

    private void TrySubscribeEvents()
    {
        if (_subscribedToEvents) return;
        if (_eventCenter == null) return;
        _eventCenter.OnShowUI += OnShowUI;
        _eventCenter.OnHideUI += OnHideUI;
        _eventCenter.OnToggleUI += OnToggleUI;
        _subscribedToEvents = true;
    }

    private void TryUnsubscribeEvents()
    {
        if (!_subscribedToEvents) return;
        if (_eventCenter != null)
        {
            _eventCenter.OnShowUI -= OnShowUI;
            _eventCenter.OnHideUI -= OnHideUI;
            _eventCenter.OnToggleUI -= OnToggleUI;
        }
        _subscribedToEvents = false;
    }
    #endregion
    //==============================================================================//
    //                                                                              //
    //                                 事件系统                                     //
    //                                                                              //
    //==============================================================================//
    #region 事件系统
    private void OnShowUI(object sender, UIShowEventArgs args)
    {
        ShowUI(args?.UITypes);
    }

    private void OnHideUI(object sender, UIHideEventArgs args)
    {
        HideUI(args?.UITypes);
    }

    private void OnToggleUI(object sender, UIToggleEventArgs args)
    {
        ToggleUI(args?.UITypes);
    }
    #endregion
    //==============================================================================//
    //                                                                              //
    //                                 节点管理                                     //
    //                                                                              //
    //==============================================================================//
    #region 节点管理
    // 显式注册并指定类型（推荐用于运行时动态创建的 UI）
    public void RegisterUIRoot(GameObject root, UIRootType type)
    {
        if (root == null || type == UIRootType.None) return;
        _rootsByType[type] = root;
    }

    public void UnregisterUIRoot(GameObject root)
    {
        if (root == null) return;
        // 清理枚举映射中引用相同的项
        var keysToRemove = new List<UIRootType>();
        foreach (var kv in _rootsByType)
        {
            if (kv.Value == root) keysToRemove.Add(kv.Key);
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
            if (kv.Value != null && kv.Value.activeSelf) result.Add(kv.Key);
        }
        return result;
    }

    // 从 ScriptableObject DB 初始化内部映射
    private void InitializeFromDatabase()
    {
        _rootsByType.Clear();
        if (_uiRootDatabase == null) return;
        foreach (var e in _uiRootDatabase.GetAllEntries())
        {
            if (e.Type == UIRootType.None) continue;
            var proto = e.Root;
            if (proto == null) continue;

            GameObject instance = null;
            // If the referenced object is a scene instance, use it directly
            if (proto.scene.IsValid())
            {
                instance = proto;
            }
            else
            {
                // Try find an existing scene object with the same name
                instance = GameObject.Find(proto.name);
                if (instance == null)
                {
                    // instantiate prefab into scene
                    instance = Object.Instantiate(proto);
                    instance.name = proto.name; // remove (Clone) for clarity
                }
            }

            if (instance != null) _rootsByType[e.Type] = instance;
        }
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
        if (_rootsByType.TryGetValue(type, out var go) && go != null) return go.activeSelf;
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
            foreach (var kv in _rootsByType) if (kv.Value != null) kv.Value.SetActive(true);
            return;
        }
        foreach (var t in types)
        {
            if (t == UIRootType.None) continue;
            if (_rootsByType.TryGetValue(t, out var go) && go != null) go.SetActive(true);
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
            foreach (var kv in _rootsByType) if (kv.Value != null) kv.Value.SetActive(false);
            return;
        }
        foreach (var t in types)
        {
            if (t == UIRootType.None) continue;
            if (_rootsByType.TryGetValue(t, out var go) && go != null) go.SetActive(false);
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
                kv.Value.SetActive(!kv.Value.activeSelf);
            }
            return;
        }
        foreach (var t in types)
        {
            if (t == UIRootType.None) continue;
            if (_rootsByType.TryGetValue(t, out var go) && go != null) go.SetActive(!go.activeSelf);
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