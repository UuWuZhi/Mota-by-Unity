using System;
using Modules.Core.Runtime;
using Modules.EventNodeSystem.DataDefine;
using Modules.EventNodeSystem.DataDefine.Context;
using Modules.EventNodeSystem.Runtime.Nodes.Condition.Data;
using Modules.Player.Runtime.Attribute;
using UnityEngine;

namespace Modules.EventNodeSystem.Runtime.Nodes.Condition
{
    /// <summary>
    ///     玩家持有属性条件节点模板。
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerHasAttribute", menuName = "EventNodes/Condition/PlayerHasAttribute")]
    public class PlayerHasAttribute : ConditionNode
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
        ///     使用节点数据执行条件判断。
        /// </summary>
        /// <param name="data">节点数据。</param>
        /// <param name="ctx">执行上下文。</param>
        /// <param name="onResult">条件结果回调。</param>
        public override void Evaluate(BaseNodeData data, EventNodeContext ctx, Action<bool> onResult)
        {
            if (data is not PlayerHasAttributeData conditionData)
            {
                DebugEditor.LogWarning(
                    $"{nameof(PlayerHasAttribute)}: 数据类型不匹配，期望 {nameof(PlayerHasAttributeData)}，默认返回 false。");
                onResult?.Invoke(false);
                return;
            }

            bool result;
            try
            {
                var playerAttribute = ctx?.GetService<PlayerAttribute>();
                if (!playerAttribute)
                {
                    DebugEditor.LogWarning($"[{nameof(PlayerHasAttribute)}]: PlayerAttribute 未配置，默认返回 false。");
                    onResult?.Invoke(false);
                    return;
                }

                // 获取实际属性值并根据比较模式判断
                var actualValue = playerAttribute.GetAttributeValue(conditionData.attributeType);
                result = conditionData.comparisonMode switch
                {
                    ComparisonMode.Greater => actualValue > conditionData.requiredValue,
                    ComparisonMode.GreaterOrEqual => actualValue >= conditionData.requiredValue,
                    ComparisonMode.Less => actualValue < conditionData.requiredValue,
                    ComparisonMode.LessOrEqual => actualValue <= conditionData.requiredValue,
                    ComparisonMode.Equal => actualValue == conditionData.requiredValue,
                    ComparisonMode.NotEqual => actualValue != conditionData.requiredValue,
                    _ => actualValue >= conditionData.requiredValue
                };
            }
            catch (Exception ex)
            {
                DebugEditor.LogException(ex);
                DebugEditor.LogError($"[PlayerHasAttribute(PlayerHasAttribute)]: 执行条件判断时发生异常，默认返回 false。{ex.Message}");
                result = false;
            }

            DebugEditor.Log(
                $"PlayerHasAttribute: 检测玩家属性 {conditionData.attributeType} {conditionData.comparisonMode} {conditionData.requiredValue}，实际值：{ctx?.GetService<PlayerAttribute>()?.GetAttributeValue(conditionData.attributeType) ?? 0}，结果：{result}");
            onResult?.Invoke(result);
        }
    }
}