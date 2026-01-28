using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CanDefeatCondition", menuName = "EventNodes/Condition/CanDefeat")]
public class CanDefeatCondition : ConditionNode
{

    public override void Evaluate(EventNodeContext ctx, Action<bool> onResult)
    {
        //Debug.Log("Node:对战检测开始");
        bool canDefeat;
        var enemyUnit = ctx.TileObject.GetComponent<EnemyUnit>();
        if (enemyUnit == null)
        {
            Debug.LogError("BattleNode: 目标没有 EnemyUnit 组件");
            return;
        }
        // 构建玩家数据（与之前 EventManager 的做法一致）
        BattleUnitData playerData = ctx.PlayerAttribute.GetPlayerUnitData();
        BattleUnitData enemyData = enemyUnit.GetBattleUnitData();
        try
        {
            var result = BattleManager.Instance.ResolveBattle(playerData, enemyData, out int playerHPLoss);
            if (result == BattleResult.PlayerWin)
            {
                canDefeat = true;
                ctx.Vars["PlayerHPLoss"] = playerHPLoss;
                ctx.Vars["GoldReward"] = enemyUnit.enemyData.goldReward;
            }
            else 
            {
                canDefeat = false;
            }
        }
        catch { canDefeat = false; }
        //Debug.Log($"对战检测结果：{canDefeat}");
        onResult?.Invoke(canDefeat);
    }
}