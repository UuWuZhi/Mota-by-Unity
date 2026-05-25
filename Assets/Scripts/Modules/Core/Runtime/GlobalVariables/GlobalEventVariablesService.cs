using System;
using System.Collections.Generic;
using Modules.Core.DataDefine;

namespace Modules.Core.Runtime.GlobalVariables
{
    /// <summary>
    ///     全局事件变量服务，实现 IGlobalEventVariables。
    /// </summary>
    public class GlobalEventVariablesService : IGlobalEventVariables
    {
        /// <summary>
        ///     变量值类型。
        /// </summary>
        public enum ValueType
        {
            Int,
            Float,
            String,
            Bool
        }

        private readonly Dictionary<GlobalEventKey, object> _data = new();

        /// <summary>
        ///     判断指定键是否存在。
        /// </summary>
        public bool HasKey(GlobalEventKey key)
        {
            return _data.ContainsKey(key);
        }

        /// <summary>
        ///     移除指定键。
        /// </summary>
        public void Remove(GlobalEventKey key)
        {
            _data.Remove(key);
        }

        /// <summary>
        ///     设置整数值。
        /// </summary>
        public void SetInt(GlobalEventKey key, int value)
        {
            _data[key] = value;
        }

        /// <summary>
        ///     获取整数值。
        /// </summary>
        public int GetInt(GlobalEventKey key, int defaultValue = 0)
        {
            if (_data.TryGetValue(key, out var obj) && obj is int i) return i;

            return defaultValue;
        }

        /// <summary>
        ///     设置浮点值。
        /// </summary>
        public void SetFloat(GlobalEventKey key, float value)
        {
            _data[key] = value;
        }

        /// <summary>
        ///     获取浮点值。
        /// </summary>
        public float GetFloat(GlobalEventKey key, float defaultValue = 0f)
        {
            if (_data.TryGetValue(key, out var obj) && obj is float f) return f;

            return defaultValue;
        }

        /// <summary>
        ///     设置字符串值。
        /// </summary>
        public void SetString(GlobalEventKey key, string value)
        {
            _data[key] = value ?? string.Empty;
        }

        /// <summary>
        ///     获取字符串值。
        /// </summary>
        public string GetString(GlobalEventKey key, string defaultValue = "")
        {
            if (_data.TryGetValue(key, out var obj) && obj is string s) return s;

            return defaultValue;
        }

        /// <summary>
        ///     设置布尔值。
        /// </summary>
        public void SetBool(GlobalEventKey key, bool value)
        {
            _data[key] = value;
        }

        /// <summary>
        ///     获取布尔值。
        /// </summary>
        public bool GetBool(GlobalEventKey key, bool defaultValue = false)
        {
            if (_data.TryGetValue(key, out var obj) && obj is bool b) return b;

            return defaultValue;
        }

        /// <summary>
        ///     设置枚举值。
        /// </summary>
        public void SetEnum<T>(GlobalEventKey key, T value) where T : struct, Enum
        {
            _data[key] = Convert.ToInt32(value);
        }

        /// <summary>
        ///     获取枚举值。
        /// </summary>
        public T GetEnum<T>(GlobalEventKey key, T defaultValue = default) where T : struct, Enum
        {
            if (!_data.TryGetValue(key, out var obj)) return defaultValue;
            switch (obj)
            {
                case int i:
                    try
                    {
                        return (T)Enum.ToObject(typeof(T), i);
                    }
                    catch (Exception ex)
                    {
                        DebugEditor.LogError($"[GlobalEventVariablesService]:{ex}");
                    }

                    break;
                case string s when Enum.TryParse<T>(s, out var parsed):
                    return parsed;
            }

            return defaultValue;
        }
    }
}