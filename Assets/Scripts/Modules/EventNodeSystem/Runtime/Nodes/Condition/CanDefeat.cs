using System;
using Modules.Core.Runtime;
using Modules.Enemy.DataDefine;
using Modules.Enemy.Runtime;
using Modules.EventNodeSystem.DataDefine;
using Modules.EventNodeSystem.DataDefine.Context;
using Modules.Player.Runtime.Attribute;
using UnityEngine;

namespace Modules.EventNodeSystem.Runtime.Nodes.Condition
{
    /// <summary>
    ///     判断玩家是否能够击败当前敌人的瓦片条件节点。
    /// </summary>
    [CreateAssetMenu(fileName = "CanDefeat", menuName = "EventNodes/Condition/CanDefeat")]
    public class CanDefeat : TileConditionNode
    {
        /// <summary>
        ///     声明执行所需服务。
        /// </summary>
        /// <returns>所需服务类型数组。</returns>
        public override Type[] GetRequiredServices()
        {
            return new[] { typeof(PlayerAttribute) };
        }

        /// <summary>
        ///     执行瓦片条件判断。
        /// </summary>
        /// <param name="data">节点数据。</param>
        /// <param name="ctx">瓦片上下文。</param>
        /// <param name="onResult">条件结果回调。</param>
        public override void EvaluateTile(BaseNodeData data, EventNodeTileContext ctx, Action<bool> onResult)
        {
            var canDefeat = false;
            try
            {
                var enemyUnit = ctx?.TileObject?.GetComponent<EnemyUnit>();
                if (enemyUnit == null)
                {
                    DebugEditor.LogError("[CanDefeat]: 目标没有 EnemyUnit 组件。");
                    onResult?.Invoke(false);
                    return;
                }

                var playerAttribute = ctx.GetService<PlayerAttribute>();
                if (playerAttribute == null)
                {
                    DebugEditor.LogWarning("[CanDefeat]: PlayerAttribute 未配置，无法计算战斗结果。");
                    onResult?.Invoke(false);
                    return;
                }

                var playerData = playerAttribute.GetPlayerUnitData();
                var enemyData = enemyUnit.GetBattleUnitData();
                var result = BattleManager.Instance.ResolveBattle(playerData, enemyData, out var playerHpLoss);
                if (result == BattleResult.PlayerWin)
                {
                    canDefeat = true;
                    ctx.Set(ContextVarKey.PlayerHpLoss, playerHpLoss);
                    ctx.Set(ContextVarKey.GoldReward, enemyUnit.enemyData.goldReward);
                }
            }
            catch (Exception ex)
            {
                DebugEditor.LogException(ex);
                DebugEditor.LogError("[CanDefeat]: 执行条件判断时发生异常，默认返回 false。");
                canDefeat = false;
            }

            onResult?.Invoke(canDefeat);
        }
    }
}