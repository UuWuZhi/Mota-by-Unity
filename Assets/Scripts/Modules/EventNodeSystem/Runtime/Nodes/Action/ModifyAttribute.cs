using System;
using System.Collections.Generic;
using System.Linq;
using Modules.Core.DataDefine;
using Modules.Core.DataDefine.Units;
using Modules.Core.Runtime;
using Modules.EventNodeSystem.DataDefine;
using Modules.EventNodeSystem.DataDefine.Context;
using Modules.EventNodeSystem.DataDefine.Data;
using Modules.EventNodeSystem.Runtime.Nodes.Action.Data;
using Modules.Player.Runtime.Attribute;
using UnityEngine;

namespace Modules.EventNodeSystem.Runtime.Nodes.Action
{
    [CreateAssetMenu(fileName = "ModifyAttribute", menuName = "EventNodes/Action/ModifyAttribute")]
    public class ModifyAttribute : ActionNode
    {
        public override Type[] GetRequiredServices()
        {
            return new[] { typeof(PlayerAttribute) };
        }

        public override void Execute(BaseNodeData data, EventNodeContext ctx, System.Action onComplete)
        {
            if (data is not ModifyAttributeData modifyData)
            {
                DebugEditor.LogWarning("ModifyAttribute: data 类型不匹配，跳过执行。");
                onComplete?.Invoke();
                return;
            }

            var playerAttribute = ctx.GetService<PlayerAttribute>();
            if (!playerAttribute)
            {
                DebugEditor.LogError("ModifyAttribute: PlayerAttribute 未配置，无法执行。");
                onComplete?.Invoke();
                return;
            }

            if (!TryResolveParameters(modifyData, ctx, out var resolvedEntries))
            {
                onComplete?.Invoke();
                return;
            }

            try
            {
                foreach (var entry in resolvedEntries)
                    ApplyOperation(modifyData.operation, playerAttribute, entry.type, entry.value);
            }
            catch (Exception ex)
            {
                DebugEditor.LogException(ex);
            }
            finally
            {
                onComplete?.Invoke();
            }
        }

        private void ApplyOperation(ModifyOperation operation, PlayerAttribute playerAttribute,
            AttributeType resolvedType,
            int resolvedValue)
        {
            if (resolvedValue <= 0)
            {
                DebugEditor.LogWarning("ModifyAttribute: value <= 0，跳过执行。");
                return;
            }

            switch (operation)
            {
                case ModifyOperation.Add:
                    playerAttribute.AddAttribute(resolvedType, resolvedValue);
                    break;
                case ModifyOperation.Remove:
                    playerAttribute.ReduceAttribute(resolvedType, resolvedValue);
                    break;
                case ModifyOperation.Set:
                    playerAttribute.SetAttributeValue(resolvedType, resolvedValue);
                    break;
                default:
                    DebugEditor.LogWarning("ModifyAttribute: 未识别的操作类型。");
                    break;
            }
        }

        private bool TryResolveParameters(ModifyAttributeData data, EventNodeContext ctx,
            out List<(AttributeType type, int value)> resolvedEntries)
        {
            resolvedEntries = new List<(AttributeType type, int value)>();

            switch (data.parameterSource)
            {
                case ModifyParameterSource.Fixed:
                    resolvedEntries.Add((data.attributeType, data.value));
                    return true;
                case ModifyParameterSource.TileUnit:
                    return TryResolveFromTileUnit(ctx, resolvedEntries);
                case ModifyParameterSource.Vars:
                    return TryResolveFromVars(data, ctx, resolvedEntries);
                default:
                    DebugEditor.LogWarning("ModifyAttribute: 未识别的参数来源。");
                    return false;
            }
        }

        private bool TryResolveFromTileUnit(EventNodeContext ctx, List<(AttributeType type, int value)> resolvedEntries)
        {
            if (ctx is not EventNodeTileContext tileCtx || !tileCtx.TileObject)
            {
                DebugEditor.LogWarning("ModifyAttribute: TileUnit 来源需要 EventNodeTileContext 与 TileObject。");
                return false;
            }

            var unit = tileCtx.TileObject.GetComponent<AttributeUnit>();
            if (!unit || unit.attributeBonuses == null || unit.attributeBonuses.Count == 0)
            {
                DebugEditor.LogWarning("ModifyAttribute: 未找到 AttributeUnit 或数据为空。");
                return false;
            }

            resolvedEntries.AddRange(from bonus in unit.attributeBonuses
                where bonus != null
                select (bonus.type, bonus.value));

            return resolvedEntries.Count > 0;
        }

        private bool TryResolveFromVars(ModifyAttributeData data, EventNodeContext ctx,
            List<(AttributeType type, int value)> resolvedEntries)
        {
            if (ctx?.Vars == null)
            {
                DebugEditor.LogWarning("ModifyAttribute: Vars 来源需要有效的上下文。");
                return false;
            }

            if (!ctx.TryGet(data.valueVarKey, out int resolvedValue))
            {
                DebugEditor.LogWarning($"ModifyAttribute: Vars 中未找到 {data.valueVarKey}。");
                return false;
            }

            resolvedEntries.Add((data.attributeType, resolvedValue));
            return true;
        }
    }
}