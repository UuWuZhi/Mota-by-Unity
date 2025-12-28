using System.Collections.Generic;
using UnityEngine;
using VContainer; // for [Inject]

// 中央 UI 管理器：负责注册场景中所有可控制的 UI 根对象，并统一显示/隐藏
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    public List<string> RecordedUI = new List<string>();
    [SerializeField] private List<GameObject> uiRoots = new List<GameObject>();

    // injected EventCenter (via VContainer)
    private EventCenter _eventCenter;
    private bool _subscribedToEvents = false;

    //==============================================================================//
    //                                                                              //
    //                                 生命周期                                     //
    //                                                                              //
    //==============================================================================//
    #region 生命周期
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        // Try to subscribe; if DI hasn't injected EventCenter yet, Inject(...) will call TrySubscribe later
        TrySubscribeEvents();
    }

    private void OnDisable()
    {
        TryUnsubscribeEvents();
    }

    [Inject]
    public void Inject(EventCenter eventCenter)
    {
        _eventCenter = eventCenter;
        // If this object is enabled, subscribe immediately
        TrySubscribeEvents();
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
        ShowUI(args?.UINames);
    }

    private void OnHideUI(object sender, UIHideEventArgs args)
    {
        HideUI(args?.UINames);
    }

    private void OnToggleUI(object sender, UIToggleEventArgs args)
    {
        ToggleUI(args?.UINames);
    }
    #endregion
    //==============================================================================//
    //                                                                              //
    //                                 节点管理                                     //
    //                                                                              //
    //==============================================================================//
    #region 节点管理
    public void RegisterUIRoot(GameObject root)
    {
        if (root == null) return;
        if (!uiRoots.Contains(root)) uiRoots.Add(root);
    }

    public void UnregisterUIRoot(GameObject root)
    {
        if (root == null) return;
        if (uiRoots.Contains(root)) uiRoots.Remove(root);
    }
    // 新增：提供对外读取 UI 根及其显示状态的接口
    public List<string> GetActiveRootNames()
    {
        List<string> result = new List<string>();
        foreach (var r in uiRoots)
        {
            if (r == null) continue;
            if (r.activeSelf) result.Add(r.name);
        }
        return result;
    }

    public bool IsUIRootRegistered(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        foreach (var r in uiRoots) if (r != null && r.name == name) return true;
        return false;
    }

    public bool IsUIRootActive(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        foreach (var r in uiRoots) if (r != null && r.name == name) return r.activeSelf;
        return false;
    }
    #endregion
    //==============================================================================//
    //                                                                              //
    //                                 显示方法                                     //
    //                                                                              //
    //==============================================================================//
    #region 显示方法
    public void ShowUI(List<string> names)
    {
        if (names == null || names.Count == 0)
        {
            foreach (var r in uiRoots) if (r != null) r.SetActive(true);
            return;
        }
        foreach (var name in names)
        {
            foreach (var r in uiRoots)
            {
                if (r == null) continue;
                if (r.name == name)
                {
                    r.SetActive(true);
                    break;
                }
            }
        }
    }

    public void ShowUI(string name)
    {
        if (string.IsNullOrEmpty(name)) { ShowUI((List<string>)null); return; }
        ShowUI(new List<string> { name });
    }

    public void HideUI(List<string> names)
    {
        if (names == null || names.Count == 0)
        {
            foreach (var r in uiRoots) if (r != null) r.SetActive(false);
            return;
        }
        foreach (var name in names)
        {
            foreach (var r in uiRoots)
            {
                if (r == null) continue;
                if (r.name == name)
                {
                    r.SetActive(false);
                    break;
                }
            }
        }
    }
    public void HideUI(string name)
    {
        if (string.IsNullOrEmpty(name)) { HideUI((List<string>)null); return; }
        HideUI(new List<string> { name });
    }

    public void ToggleUI(List<string> names)
    {
        if (names == null || names.Count == 0)
        {
            foreach (var r in uiRoots)
            {
                if (r == null) continue;
                r.SetActive(!r.activeSelf);
            }
            return;
        }

        foreach (var name in names)
        {
            foreach (var r in uiRoots)
            {
                if (r == null) continue;
                if (r.name == name)
                {
                    r.SetActive(!r.activeSelf);
                    break;
                }
            }
        }
    }

    public void ToggleUI(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            ToggleUI((List<string>)null);
            return;
        }
        ToggleUI(new List<string> { name });
    }
    // 新增：封装 “隐藏并记录当前可见 UI” 与 “显示记录的 UI” 的方法
    // HideAndRecordVisible: 返回被隐藏的 UI 名称列表（可能为空）
    public List<string> HideAndRecordVisible()
    {
        var active = GetActiveRootNames();
        if (active != null && active.Count > 0)
        {
            HideUI(active);
        }
        RecordedUI = active;
        return active ?? new List<string>();
    }

    // ShowRecordedVisible: 传入先前记录的名称列表，尝试恢复显示其中注册过的 UI 根
    public void ShowRecordedVisible(List<string> recordedNames)
    {
        if (recordedNames == null || recordedNames.Count == 0) return;
        var restore = new List<string>();
        foreach (var name in recordedNames)
        {
            if (string.IsNullOrEmpty(name)) continue;
            if (IsUIRootRegistered(name)) restore.Add(name);
        }
        if (restore.Count > 0) ShowUI(restore);
    }
    public void ShowRecordedVisible()
    {
        ShowRecordedVisible(RecordedUI);
    }
    #endregion
}