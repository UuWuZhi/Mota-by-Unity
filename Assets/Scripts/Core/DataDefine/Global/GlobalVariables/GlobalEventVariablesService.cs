using System;
using System.Collections.Generic;

public class GlobalEventVariablesService : IGlobalEventVariables
{
    public enum ValueType { Int, Float, String, Bool }
    private readonly Dictionary<GlobalEventKey, object> _data = new Dictionary<GlobalEventKey, object>();
    //==============================================================================//
    //                                 General                                      //
    //==============================================================================//
    public bool HasKey(GlobalEventKey key) => _data.ContainsKey(key);
    public void Remove(GlobalEventKey key) { if (_data.ContainsKey(key)) _data.Remove(key); }
    //==============================================================================//
    //                                 Int                                          //
    //==============================================================================//
    public void SetInt(GlobalEventKey key, int value)
    {
        _data[key] = value;
    }
    public int GetInt(GlobalEventKey key, int defaultValue = 0)
    {
        if (_data.TryGetValue(key, out var obj) && obj is int i) return i;
        return defaultValue;
    }
    //==============================================================================//
    //                                 Float                                        //
    //==============================================================================//
    public void SetFloat(GlobalEventKey key, float value)
    {
        _data[key] = value;
    }
    public float GetFloat(GlobalEventKey key, float defaultValue = 0f)
    {
        if (_data.TryGetValue(key, out var obj) && obj is float f) return f;
        return defaultValue;
    }
    //==============================================================================//
    //                                 String                                       //
    //==============================================================================//
    public void SetString(GlobalEventKey key, string value)
    {
        _data[key] = value ?? string.Empty;
    }
    public string GetString(GlobalEventKey key, string defaultValue = "")
    {
        if (_data.TryGetValue(key, out var obj) && obj is string s) return s;
        return defaultValue;
    }
    //==============================================================================//
    //                                 Bool                                         //
    //==============================================================================//
    public void SetBool(GlobalEventKey key, bool value)
    {
        _data[key] = value;
    }
    public bool GetBool(GlobalEventKey key, bool defaultValue = false)
    {
        if (_data.TryGetValue(key, out var obj) && obj is bool b) return b;
        return defaultValue;
    }

    //==============================================================================//
    //                                 Enum                                         //
    //==============================================================================//
    public void SetEnum<T>(GlobalEventKey key, T value) where T : struct, Enum
    {
        _data[key] = Convert.ToInt32(value);
    }

    public T GetEnum<T>(GlobalEventKey key, T defaultValue = default) where T : struct, Enum
    {
        if (_data.TryGetValue(key, out var obj))
        {
            if (obj is int i)
            {
                try
                {
                    return (T)Enum.ToObject(typeof(T), i);
                }
                catch { }
            }
            // if stored as string, try parse
            if (obj is string s && Enum.TryParse<T>(s, out var parsed))
            {
                return parsed;
            }
        }
        return defaultValue;
    }
}
