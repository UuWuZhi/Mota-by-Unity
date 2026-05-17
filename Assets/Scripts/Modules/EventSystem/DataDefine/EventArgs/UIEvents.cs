using System.Collections.Generic;

namespace Modules.EventSystem.DataDefine.EventArgs
{
    // 使用枚举来标识 UI 根（替换原来的字符串列表）。为兼容性保留按名称访问的只读属性。
    public class UIShowEventArgs : System.EventArgs
    {
        // 要显示的 UI 列表（null 表示显示全部）
        public List<string> UITypes { get; set; }

        public string Type => UITypes is { Count: > 0 } ? UITypes[0] : string.Empty;
    }

    public class UIHideEventArgs : System.EventArgs
    {
        public List<string> UITypes { get; set; }
        public string Type => UITypes is { Count: > 0 } ? UITypes[0] : string.Empty;
    }

    public class UIToggleEventArgs : System.EventArgs
    {
        public List<string> UITypes { get; set; }
        public string Type => UITypes is { Count: > 0 } ? UITypes[0] : string.Empty;
    }
}