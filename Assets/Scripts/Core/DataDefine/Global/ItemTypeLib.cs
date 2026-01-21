//==============================================================================//
//                                 道具类型                                      //
//==============================================================================//
/// <summary>
/// 道具类型枚举（用于区分不同道具的功能）
/// </summary>
public enum ItemType
{
    None,       // 仅用于更新与排错
    All,        // 全部类型（用于批量操作）
    // 钥匙类
    Key_Red,    // 红钥匙
    Key_Blue,   // 蓝钥匙
    Key_Yellow, // 黄钥匙
    // 血瓶类
    PotionHP,  // 血瓶
    // 宝石类
    Gem,        // 宝石
    // 幸运金币（存在则掉落金币翻倍）
    Lucky_Gold,
    MonsterBook // 怪物图鉴
}

/// <summary>
/// 道具加成数据结构（用于配置道具效果）
/// </summary>
[System.Serializable]
public class ItemBonus
{
    public ItemType type;   // 道具类型
    public int value;       // 道具效果值（如恢复量/数量）
}