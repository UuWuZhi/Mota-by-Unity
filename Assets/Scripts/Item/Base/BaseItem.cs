using UnityEngine;

/// <summary>
/// 道具使用逻辑基类（ScriptableObject）。
/// 子类实现具体 Use 行为。返回 true 表示使用后应被消耗（背包中数量减 1）。
/// </summary>
public abstract class BaseItem : ScriptableObject
{
    /// <summary>
    /// 执行道具效果（无上下文版本）。
    /// 注意：当前版本不传入运行时 Context，后续可扩展重载或通过服务调用场景对象。
    /// 返回 true 表示道具被消耗。
    /// </summary>
    public virtual bool Use()
    {
        Debug.LogWarning($"BaseItem.Use: {name} 未实现具体逻辑");
        return false;
    }
}
