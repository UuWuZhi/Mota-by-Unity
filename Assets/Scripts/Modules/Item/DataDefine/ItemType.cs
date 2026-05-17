//==============================================================================//
//                                 道具类型                                      //
//==============================================================================//

using System;

namespace Modules.Item.DataDefine
{
    /// <summary>
    ///     道具类型枚举（用于区分不同道具的功能）
    /// </summary>
    public enum ItemType
    {
        None, // 仅用于更新与排错
        All, // 全部类型（用于批量操作）
        KeyRed, // 红钥匙
        KeyBlue, // 蓝钥匙
        KeyYellow, // 黄钥匙
        PotionHp, // 血瓶
        Gem, // 宝石
        LuckyGold, // 幸运金币
        MonsterBook, // 怪物图鉴
        Pickaxe // 镐
    }

    /// <summary>
    ///     道具加成数据结构（用于配置道具效果）
    /// </summary>
    [Serializable]
    public class ItemBonus
    {
        public ItemType type; // 道具类型
        public int value; // 道具效果值（如恢复量/数量）
    }
}