using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 玩家属性管理器（单例）：管理玩家的HP、攻击、防御等基础属性
/// </summary>
public class PlayerAttribute : MonoBehaviour
{
    [Header("初始属性配置")]
    [Tooltip("初始HP")] public int initHP = 100;
    [Tooltip("初始攻击")] public int initAttack = 10;
    [Tooltip("初始防御")] public int initDefense = 5;
    [Tooltip("初始金钱")] public int initGold = 0;

    // 用字典存储属性值（键：属性类型，值：属性值）
    private Dictionary<AttributeType, int> _attributes = new Dictionary<AttributeType, int>();

    // 公开只读属性（供UI等外部访问）
    public int CurrentHP => GetAttributeValue(AttributeType.HP);
    public int Attack => GetAttributeValue(AttributeType.Attack);
    public int Defense => GetAttributeValue(AttributeType.Defense);
    public int Gold => GetAttributeValue(AttributeType.Gold);

    /// <summary>
    /// 重置属性为初始值（新游戏/复活时调用）
    /// </summary>
    public void ResetAttribute()
    {
        _attributes[AttributeType.HP] = initHP;
        _attributes[AttributeType.Attack] = initAttack;
        _attributes[AttributeType.Defense] = initDefense;
        _attributes[AttributeType.Gold] = initGold;

        // 通过事件中心触发全属性更新事件
        EventCenter.Instance.TriggerAttributeChanged(new AttributeChangedEventArgs
        {
            ChangedType = AttributeType.All
        });
    }

    /// <summary>
    /// 获取指定属性的值
    /// </summary>
    public int GetAttributeValue(AttributeType type)
    {
        if (_attributes.TryGetValue(type, out int value))
        {
            return value;
        }
        Debug.LogError($"未找到属性类型：{type}");
        return 0;
    }
    /// <summary>
    /// 具有指定属性的值
    /// </summary>
    public bool HasAttributeValue(AttributeType type, int value)
    {
        return (GetAttributeValue(type) >= value);
    }
    /// <summary>
    /// 增加指定属性的值
    /// </summary>
    /// <param name="type">属性类型</param>
    /// <param name="value">增加的值（必须为正数）</param>
    public void AddAttribute(AttributeType type, int value)
    {
        if (type == AttributeType.All)
        {
            Debug.LogError("不能对All类型执行增加操作");
            return;
        }
        if (value <= 0) return;

        _attributes[type] += value;
        Debug.Log($"{type}+{value}！当前{type}：{_attributes[type]}");
        // 通过事件中心触发属性变化事件
        EventCenter.Instance.TriggerAttributeChanged(new AttributeChangedEventArgs
        {
            ChangedType = type
        });
    }
    // 新增减少属性的方法（用于扣血）
    public void ReduceAttribute(AttributeType type, int value)
    {
        if (type == AttributeType.All)
        {
            Debug.LogError("不能对All类型执行减少操作");
            return;
        }
        if (value <= 0) return;

        _attributes[type] = Mathf.Max(0, _attributes[type] - value);
        Debug.Log($"{type}-{value}！当前{type}：{_attributes[type]}");
        
        // 通过事件中心触发属性变化事件
        EventCenter.Instance.TriggerAttributeChanged(new AttributeChangedEventArgs
        {
            ChangedType = type
        });
    }
    public BattleUnitData GetPlayerUnitData()
    {
        return new BattleUnitData
        {
            currentHP = CurrentHP,
            attack = Attack,
            defense = Defense
        };
    }
}