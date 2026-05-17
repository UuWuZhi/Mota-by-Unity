namespace Modules.UI.DataDefine
{
    /// <summary>
    ///     表示应用程序用户界面的当前显示状态。
    /// </summary>
    /// <remarks>用于在视图或状态机中区分主界面、隐藏、菜单和对话框四种显示模式；通常与事件、数据绑定或导航逻辑一起使用以更新 UI。</remarks>
    public enum UIState
    {
        Main,
        Hidden,
        Menu,
        Dialog
    }
}