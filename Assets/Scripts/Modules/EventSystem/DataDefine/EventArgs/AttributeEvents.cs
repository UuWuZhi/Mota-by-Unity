using Modules.Core.DataDefine;

namespace Modules.EventSystem.DataDefine.EventArgs
{
    /// <summary>
    ///     属性变化事件参数
    /// </summary>
    public class AttributeChangedEventArgs : System.EventArgs
    {
        public AttributeType ChangedType { get; set; } // 变化的属性类型
    }
}