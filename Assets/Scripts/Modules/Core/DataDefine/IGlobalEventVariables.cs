using System;

namespace Modules.Core.DataDefine
{
    /// <summary>
    ///     表示按 GlobalEventKey 键存取全局事件变量的契约。支持设置与获取整型、浮点、字符串、布尔和泛型枚举类型的值，并提供键存在检查与删除操作；获取器接受可选默认值。
    /// </summary>
    /// <remarks>实现应决定具体的存储策略（例如内存或持久化）并在并发访问场景下保证线程安全。获取方法在键不存在时返回调用方提供的默认值。泛型枚举方法通过约束确保类型安全。</remarks>
    public interface IGlobalEventVariables
    {
        bool HasKey(GlobalEventKey key);
        void Remove(GlobalEventKey key);

        void SetInt(GlobalEventKey key, int value);
        int GetInt(GlobalEventKey key, int defaultValue = 0);

        void SetFloat(GlobalEventKey key, float value);
        float GetFloat(GlobalEventKey key, float defaultValue = 0f);

        void SetString(GlobalEventKey key, string value);
        string GetString(GlobalEventKey key, string defaultValue = "");

        void SetBool(GlobalEventKey key, bool value);
        bool GetBool(GlobalEventKey key, bool defaultValue = false);

        // Enum helpers
        void SetEnum<T>(GlobalEventKey key, T value) where T : struct, Enum;
        T GetEnum<T>(GlobalEventKey key, T defaultValue = default) where T : struct, Enum;
    }
}