using System;
using Modules.Core.Runtime;
using Modules.EventNodeSystem.DataDefine;
using Modules.EventNodeSystem.DataDefine.Context;
using Modules.EventNodeSystem.Runtime.Nodes.Action.Data;
using UnityEngine;

namespace Modules.EventNodeSystem.Runtime.Nodes.Action
{
    [CreateAssetMenu(fileName = "SetVariable", menuName = "EventNodes/Action/SetVariable")]
    public class SetVariable : ActionNode
    {
        public override void Execute(BaseNodeData data, EventNodeContext ctx, System.Action onComplete)
        {
            var setData = data as SetVariableData;
            if (setData == null)
            {
                DebugEditor.LogWarning("SetVariable: data 类型不匹配，跳过执行。");
                onComplete?.Invoke();
                return;
            }

            if (ctx == null)
            {
                onComplete?.Invoke();
                return;
            }

            try
            {
                switch (setData.varType)
                {
                    case SetVariableData.VarType.Bool:
                    {
                        var current = false;
                        if (ctx.TryGet(setData.key, out bool b)) current = b;
                        var result = setData.operation switch
                        {
                            SetVariableData.Operation.Set => setData.boolValue,
                            SetVariableData.Operation.Toggle => !current,
                            _ => setData.boolValue
                        };
                        ctx.Set(setData.key, result);
                    }
                        break;
                    case SetVariableData.VarType.Int:
                    {
                        var current = 0;
                        if (ctx.TryGet(setData.key, out int iv)) current = iv;
                        var result = setData.operation switch
                        {
                            SetVariableData.Operation.Set => setData.intValue,
                            SetVariableData.Operation.Increment => current + setData.intValue,
                            SetVariableData.Operation.Decrement => current - setData.intValue,
                            _ => setData.intValue
                        };
                        ctx.Set(setData.key, result);
                    }
                        break;
                    case SetVariableData.VarType.Float:
                    {
                        var current = 0f;
                        if (ctx.TryGet(setData.key, out float fv)) current = fv;
                        var result = setData.operation switch
                        {
                            SetVariableData.Operation.Set => setData.floatValue,
                            SetVariableData.Operation.Increment => current + setData.floatValue,
                            SetVariableData.Operation.Decrement => current - setData.floatValue,
                            _ => setData.floatValue
                        };
                        ctx.Set(setData.key, result);
                    }
                        break;
                    case SetVariableData.VarType.String:
                    {
                        ctx.Set(setData.key, setData.stringValue ?? string.Empty);
                    }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception ex)
            {
                DebugEditor.LogException(ex);
            }

            onComplete?.Invoke();
        }
    }
}