using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CanDefeatCondition", menuName = "EventNodes/Condition/CanDefeat")]
public class CanDefeatCondition : TileConditionNode
{
    public override Type[] GetRequiredServices()
    {
        return new[] { typeof(PlayerAttribute) };
    }

    public override void EvaluateTile(EventNodeTileContext ctx, Action<bool> onResult)
    {
        //Debug.Log("Node:对战检测开始");
        bool canDefeat;
        var enemyUnit = ctx.TileObject.GetComponent<EnemyUnit>();
        if (enemyUnit == null)
        {
            Debug.LogError("BattleNode: 目标没有 EnemyUnit 组件");
            onResult?.Invoke(false);
            return;
        }
        // 构建玩家数据（与之前 EventManager 的做法一致）
        var playerAttribute = ctx?.GetService<PlayerAttribute>();
        if (playerAttribute == null)
        {
            Debug.LogWarning("CanDefeatCondition: PlayerAttribute 未配置，无法计算战斗结果。");
            onResult?.Invoke(false);
            return;
        }
        BattleUnitData playerData = playerAttribute.GetPlayerUnitData();
        BattleUnitData enemyData = enemyUnit.GetBattleUnitData();
        try
        {
            var result = BattleManager.Instance.ResolveBattle(playerData, enemyData, out int playerHPLoss);
            if (result == BattleResult.PlayerWin)
            {
                canDefeat = true;
                ctx.Set(ContextVarKey.PlayerHPLoss, playerHPLoss);
                ctx.Set(ContextVarKey.GoldReward, enemyUnit.enemyData.goldReward);
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