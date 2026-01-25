using System;
using System.Collections.Generic;

// 使用枚举来标识 UI 根（替换原来的字符串列表）。为兼容性保留按名称访问的只读属性。
public class UIShowEventArgs : EventArgs
{
    // 要显示的 UI 列表（null 表示显示全部）
    public List<UIRootType> UITypes { get; set; }

    public UIRootType Type => (UITypes != null && UITypes.Count > 0) ? UITypes[0] : UIRootType.None;
}

public class UIHideEventArgs : EventArgs
{
    public List<UIRootType> UITypes { get; set; }
    public UIRootType Type => (UITypes != null && UITypes.Count > 0) ? UITypes[0] : UIRootType.None;
}

public class UIToggleEventArgs : EventArgs
{
    public List<UIRootType> UITypes { get; set; }
    public UIRootType Type => (UITypes != null && UITypes.Count > 0) ? UITypes[0] : UIRootType.None;
}
