using UnityEngine;
using System.Collections.Generic;

// 负责识别与 UI 相关的按键（例如 F4 隐藏所有 UI）并通过 EventCenter 发布事件
public class UIInputManager : MonoBehaviour
{
    private static UIInputManager _instance;
    public static UIInputManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("UIInputManager");
                _instance = go.AddComponent<UIInputManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void Update()
    {
        // 按下 F4 隐藏全部 UI（通过传入 null 或空列表）
        if (Input.GetKeyDown(KeyCode.F4))
        {
            EventCenter.Instance?.TriggerHideUI(new UIHideEventArgs { UINames = null });
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            EventCenter.Instance?.TriggerToggleUI(new UIToggleEventArgs
            { 
                UINames = new List<string> { "Left", "Right", "Top", "Bottom", "MonsterBook", "SideMenu" } 
                });
        }

        // 未来可加入 Esc/Tab 等按键来切换背包/地图等
    }
}
