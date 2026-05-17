using System;

namespace Modules.Core.DataDefine
{
    /// <summary>
    ///     属性类型枚举（玩家/敌人的基础属性）
    /// </summary>
    public enum AttributeType
    {
        All, // 初始化用（全属性更新）
        Hp, // 血量
        Attack, // 攻击
        Defense, // 防御
        Gold // 金币
    }

    /// <summary>
    ///     属性加成数据结构（用于配置属性变化）
    /// </summary>
    [Serializable]
    public class AttributeBonus
    {
        public AttributeType type; // 属性类型
        public int value; // 属性变化值
    }
}