using UnityEngine;

/// <summary>
/// 敌人单位组件：挂载在敌人 GameObject 上，存储战斗相关属性和扩展字段
/// 可随项目需求继续扩展（速度、护甲、先攻、多段攻击、词条等）
/// </summary>
public class EnemyUnit : MonoBehaviour
{
    [Header("基础战斗属性")]
    public BattleUnitData BaseStats = new BattleUnitData
    {
        currentHP = 10,
        attack = 5,
        defense = 0
    };
    [Header("扩展字段")]
    public int goldReward = 25;    // 击败后奖励金币

    /// <summary>
    /// 获取用于 BattleManager 计算的基础 BattleUnitData（会复制一份）
    /// 将来可以把复杂的词条/逻辑在此方法中折算为最终的 BattleUnitData
    /// </summary>
    public BattleUnitData GetBattleUnitData()
    {
        return new BattleUnitData
        {
            currentHP = BaseStats.currentHP,
            attack = BaseStats.attack,
            defense = BaseStats.defense
        };
    }
}