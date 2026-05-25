using System;
using Modules.Core.Runtime;
using Modules.EventNodeSystem.DataDefine;
using Modules.EventNodeSystem.DataDefine.Context;
using Modules.EventNodeSystem.Runtime.Nodes.Condition.Data;
using Modules.Player.DataDefine;
using UnityEngine;

namespace Modules.EventNodeSystem.Runtime.Nodes.Condition
{
    /// <summary>
    ///     玩家持有物品条件节点模板。
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerHasItem", menuName = "EventNodes/Condition/PlayerHasItem")]
    public class PlayerHasItem : ConditionNode
    {
        /// <summary>
        ///     声明执行所需服务。
        /// </summary>
        /// <returns>所需服务类型数组。</returns>
        public override Type[] GetRequiredServices()
        {
            return new[] { typeof(IInventoryService) };
        }

        /// <summary>
        ///     使用节点数据执行条件判断。
        /// </summary>
        /// <param name="data">节点数据。</param>
        /// <param name="ctx">执行上下文。</param>
        /// <param name="onResult">条件结果回调。</param>
        public override void Evaluate(BaseNodeData data, EventNodeContext ctx, Action<bool> onResult)
        {
            if (data is not PlayerHasItemData conditionData)
            {
                DebugEditor.LogWarning($"{nameof(PlayerHasItem)}: 数据类型不匹配，期望 {nameof(PlayerHasItemData)}，默认返回 false。");
                onResult?.Invoke(false);
                return;
            }

            bool result;
            try
            {
                var inventoryService = ctx?.GetService<IInventoryService>();
                if (inventoryService == null)
                {
                    DebugEditor.LogError($"[{nameof(PlayerHasItem)}]: InventoryService 未配置，无法判断玩家物品数量。");
                    onResult?.Invoke(false);
                    return;
                }

                var actualCount = inventoryService.GetItemCount(conditionData.itemType);
                result = conditionData.comparisonMode switch
                {
                    ComparisonMode.Greater => actualCount > conditionData.requiredCount,
                    ComparisonMode.GreaterOrEqual => actualCount >= conditionData.requiredCount,
                    ComparisonMode.Less => actualCount < conditionData.requiredCount,
                    ComparisonMode.LessOrEqual => actualCount <= conditionData.requiredCount,
                    ComparisonMode.Equal => actualCount == conditionData.requiredCount,
                    ComparisonMode.NotEqual => actualCount != conditionData.requiredCount,
                    _ => actualCount >= conditionData.requiredCount
                };
            }
            catch (Exception ex)
            {
                DebugEditor.LogException(ex);
                DebugEditor.LogError($"[PlayerHasItem]: 执行条件判断时发生异常，默认返回 false。{ex.Message}");
                result = false;
            }

            DebugEditor.Log(
                $"PlayerHasItem: 检测玩家物品 {conditionData.itemType} 实际数量：{ctx?.GetService<IInventoryService>()?.GetItemCount(conditionData.itemType) ?? 0}，比较：{conditionData.comparisonMode} {conditionData.requiredCount}，结果：{result}");
            onResult?.Invoke(result);
        }
    }
}