using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家属性管理器（单例）：管理玩家的HP、攻击、防御等基础属性
/// </summary>
public class PlayerAttribute : MonoBehaviour
{
    [Tooltip("初始属性列表")]
    [SerializeField] private List<AttributeBonus> InitAttributes = new List<AttributeBonus>();
    private Dictionary<AttributeType, int> _attributes; // 用字典存储属性值（键：属性类型，值：属性值）

    public event EventHandler<AttributeChangedEventArgs> AttributeChanged;

    /// <summary>
    /// 重置属性为初始值（新游戏/复活时调用）
    /// </summary>
    public void ResetAttribute()
    {
        _attributes = new Dictionary<AttributeType, int>();
        foreach (var attr in InitAttributes)
        {
            if (attr == null || attr.Type == AttributeType.All) continue;
            if (_attributes.ContainsKey(attr.Type))
            {
                Debug.LogError($"初始化属性时发现重复的属性类型：{attr.Type}，请检查配置！");
                continue;
            }
            _attributes[attr.Type] = attr.Value;
        }

        // 通过事件中心触发全属性更新事件
        AttributeChanged?.Invoke(this, new AttributeChangedEventArgs
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
    /// 设置具体属性的值
    /// </summary>
    /// <param name="type"></param>
    /// <param name="value"></param>
    public void SetAttributeValue(AttributeType type, int value)
    {
        if (type == AttributeType.All)
        {
            Debug.LogError("不能对All类型执行设置操作");
            return;
        }
        _attributes[type] = value;
        Debug.Log($"{type}设置为{value}！");
        // 通过事件中心触发属性变化事件
        AttributeChanged?.Invoke(this, new AttributeChangedEventArgs
        {
            ChangedType = type
        });
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
        AttributeChanged?.Invoke(this, new AttributeChangedEventArgs
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
        AttributeChanged?.Invoke(this, new AttributeChangedEventArgs
        {
            ChangedType = type
        });
    }
    public BattleUnitData GetPlayerUnitData()
    {
        return new BattleUnitData
        {
            currentHP = GetAttributeValue(AttributeType.HP),
            attack = GetAttributeValue(AttributeType.Attack),
            defense = GetAttributeValue(AttributeType.Defense)
        };
    }
}