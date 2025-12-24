using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局事件变量存储（简单字典单例）
/// 用于跨节点 / 跨 Tile 的共享状态（非持久化）
/// 可在 Inspector 中配置常用变量（基本类型）
/// </summary>
public class GlobalEventVariables : MonoBehaviour
{
    public static GlobalEventVariables Instance { get; private set; }

    // 运行时使用的字典（键 -> object），仅在运行时读写
    public Dictionary<string, object> _data = new Dictionary<string, object>();

    // 为了在 Inspector 中支持编辑，提供一个可序列化的简单条目列表（支持 int/float/string/bool）
    public enum ValueType { Int, Float, String, Bool }

    [System.Serializable]
    public class Entry
    {
        public string Key;
        public ValueType Type = ValueType.Int;
        public int IntValue;
        public float FloatValue;
        public string StringValue;
        public bool BoolValue;
    }

    [Header("Inspector Variables (同步到运行时字典)")]
    public List<Entry> inspectorEntries = new List<Entry>();


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);

        // 将 Inspector 中的值同步到运行时字典
        SyncFromInspector();
    }

    private void SyncFromInspector()
    {
        _data.Clear();
        foreach (var e in inspectorEntries)
        {
            if (string.IsNullOrEmpty(e.Key)) continue;
            switch (e.Type)
            {
                case ValueType.Int:
                    _data[e.Key] = e.IntValue;
                    break;
                case ValueType.Float:
                    _data[e.Key] = e.FloatValue;
                    break;
                case ValueType.String:
                    _data[e.Key] = e.StringValue ?? string.Empty;
                    break;
                case ValueType.Bool:
                    _data[e.Key] = e.BoolValue;
                    break;
            }
        }
    }

    // Utility: if someone changes values at runtime via SetX, keep inspectorEntries in sync when possible
    private void SyncToInspector(string key, object value)
    {
        if (string.IsNullOrEmpty(key)) return;
        var entry = inspectorEntries.Find(x => x.Key == key);
        if (entry == null) return;
        switch (entry.Type)
        {
            case ValueType.Int:
                if (value is int vi) entry.IntValue = vi;
                break;
            case ValueType.Float:
                if (value is float vf) entry.FloatValue = vf;
                break;
            case ValueType.String:
                if (value is string vs) entry.StringValue = vs;
                break;
            case ValueType.Bool:
                if (value is bool vb) entry.BoolValue = vb;
                break;
        }
    }

    public bool HasKey(string key) => _data.ContainsKey(key);
    public void Remove(string key) { if (_data.ContainsKey(key)) _data.Remove(key); }

    // Typed getters/setters for common types
    public void SetInt(string key, int value)
    {
        int old = 0;
        if (_data.TryGetValue(key, out var obj) && obj is int oi) old = oi;

        _data[key] = value;
        SyncToInspector(key, value);

    }

    public int GetInt(string key, int defaultValue = 0)
    {
        if (_data.TryGetValue(key, out var obj) && obj is int i) return i;
        return defaultValue;
    }

    public void SetFloat(string key, float value)
    {
        _data[key] = value;
        SyncToInspector(key, value);
    }

    public float GetFloat(string key, float defaultValue = 0f)
    {
        if (_data.TryGetValue(key, out var obj) && obj is float f) return f;
        return defaultValue;
    }

    public void SetString(string key, string value)
    {
        _data[key] = value ?? string.Empty;
        SyncToInspector(key, value);
    }

    public string GetString(string key, string defaultValue = "")
    {
        if (_data.TryGetValue(key, out var obj) && obj is string s) return s;
        return defaultValue;
    }

    public void SetBool(string key, bool value)
    {
        _data[key] = value;
        SyncToInspector(key, value);
    }

    public bool GetBool(string key, bool defaultValue = false)
    {
        if (_data.TryGetValue(key, out var obj) && obj is bool b) return b;
        return defaultValue;
    }

    // 强类型 LayerId 属性（方便调用）
    public int LayerId
    {
        get => GetInt(GlobalEventKeys.LayerId, 0);
        set => SetInt(GlobalEventKeys.LayerId, value);
    }
}