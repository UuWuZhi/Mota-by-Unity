using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Data/Enemy/EnemyData", order = 1)]
public class EnemyData : ScriptableObject
{
    [Header("基础战斗属性")]
    public int id; // 唯一id，用于在数据库中查找
    public int maxHP = 10;
    public int attack = 5;
    public int defense = 0;

    [Header("额外属性")]
    public string enemyName = "Enemy";
    public int goldReward = 25;

    public BattleUnitData ToBattleUnitData()
    {
        return new BattleUnitData
        {
            currentHP = maxHP,
            attack = attack,
            defense = defense
        };
    }
}
