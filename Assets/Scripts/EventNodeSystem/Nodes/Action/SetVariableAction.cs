using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SetVariableAction", menuName = "EventNodes/Action/SetVariable")]
public class SetVariableAction : ActionNode
{
    public string key = "allowEnter";

    public enum VarType { Bool, Int, Float, String }
    public VarType varType = VarType.Bool;

    public enum Operation { Set, Increment, Decrement, Toggle }
    public Operation operation = Operation.Set;

    // values for different types
    public bool boolValue;
    public int intValue;
    public float floatValue;
    public string stringValue;

    public override void Execute(EventNodeContext ctx, Action onComplete)
    {
        if (ctx == null)
        {
            onComplete?.Invoke();
            return;
        }

        try
        {
            switch (varType)
            {
                case VarType.Bool:
                    {
                        bool current = false;
                        if (ctx.Vars.TryGetValue(key, out var o) && o is bool b) current = b;
                        bool result = current;
                        switch (operation)
                        {
                            case Operation.Set: result = boolValue; break;
                            case Operation.Toggle: result = !current; break;
                            default: result = boolValue; break;
                        }
                        ctx.Vars[key] = result;
                    }
                    break;
                case VarType.Int:
                    {
                        int current = 0;
                        if (ctx.Vars.TryGetValue(key, out var o) && o is int iv) current = iv;
                        int result = current;
                        switch (operation)
                        {
                            case Operation.Set: result = intValue; break;
                            case Operation.Increment: result = current + intValue; break;
                            case Operation.Decrement: result = current - intValue; break;
                            default: result = intValue; break;
                        }
                        ctx.Vars[key] = result;
                    }
                    break;
                case VarType.Float:
                    {
                        float current = 0f;
                        if (ctx.Vars.TryGetValue(key, out var o) && o is float fv) current = fv;
                        float result = current;
                        switch (operation)
                        {
                            case Operation.Set: result = floatValue; break;
                            case Operation.Increment: result = current + floatValue; break;
                            case Operation.Decrement: result = current - floatValue; break;
                            default: result = floatValue; break;
                        }
                        ctx.Vars[key] = result;
                    }
                    break;
                case VarType.String:
                    {
                        ctx.Vars[key] = stringValue ?? string.Empty;
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }

        onComplete?.Invoke();
    }
}