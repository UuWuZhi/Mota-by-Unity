using System;
using System.Collections.Generic;

public class UIShowEventArgs : EventArgs
{
    // 传递需要显示的 UI 名称列表；为空或 null 表示显示全部
    public List<string> UINames { get; set; }
    // 辅助属性：便捷访问单一 Name（如果只传递单个UI）
    public string Name => (UINames != null && UINames.Count > 0) ? UINames[0] : null;
}

public class UIHideEventArgs : EventArgs
{
    // 传递需要隐藏的 UI 名称列表；为空或 null 表示隐藏全部
    public List<string> UINames { get; set; }
    public string Name => (UINames != null && UINames.Count > 0) ? UINames[0] : null;
}

public class UIToggleEventArgs : EventArgs
{
    // 传递需要切换显示状态的 UI 名称列表；为空或 null 表示切换全部
    public List<string> UINames { get; set; }
    public string Name => (UINames != null && UINames.Count > 0) ? UINames[0] : null;
}
