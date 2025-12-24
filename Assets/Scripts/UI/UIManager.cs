using System.Collections.Generic;
using UnityEngine;

// 中央 UI 管理器：负责注册场景中所有可控制的 UI 根对象，并统一显示/隐藏
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    [SerializeField] private List<GameObject> uiRoots = new List<GameObject>();
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
        if (EventCenter.Instance != null)
        {
            EventCenter.Instance.OnShowUI += OnShowUI;
            EventCenter.Instance.OnHideUI += OnHideUI;
            EventCenter.Instance.OnToggleUI += OnToggleUI;
        }
    }

    private void OnDisable()
    {
        if (EventCenter.Instance != null)
        {
            EventCenter.Instance.OnShowUI -= OnShowUI;
            EventCenter.Instance.OnHideUI -= OnHideUI;
            EventCenter.Instance.OnToggleUI -= OnToggleUI;
        }
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
    #endregion
}
