using System;
using UnityEngine;

/// <summary>
/// 属性变化事件参数
/// </summary>
public class AttributeChangedEventArgs : EventArgs
{
    public AttributeType ChangedType { get; set; } // 变化的属性类型
}