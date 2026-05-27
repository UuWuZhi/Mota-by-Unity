using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Modules.Core.Runtime
{
    /// <summary>
    ///     提供仅在编辑器环境下生效的日志输出包装。
    /// </summary>
    public static class DebugEditor
    {
        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        ///     在编辑器环境下输出日志信息。
        /// </summary>
        /// <param name="message">日志内容。</param>
        /// <param name="context">关联的对象上下文。</param>
        [Conditional("UNITY_EDITOR")]
        public static void Log(object message, Object context = null)
        {
            // 仅在编辑器环境下调用日志接口
            Debug.Log(message, context);
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        ///     在编辑器环境下输出警告信息。
        /// </summary>
        /// <param name="message">警告内容。</param>
        /// <param name="context">关联的对象上下文。</param>
        [Conditional("UNITY_EDITOR")]
        public static void LogWarning(object message, Object context = null)
        {
            // 仅在编辑器环境下调用警告接口
            Debug.LogWarning(message, context);
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        ///     在编辑器环境下输出错误信息。
        /// </summary>
        /// <param name="message">错误内容。</param>
        /// <param name="context">关联的对象上下文。</param>
        [Conditional("UNITY_EDITOR")]
        public static void LogError(object message, Object context = null)
        {
            // 仅在编辑器环境下调用错误接口
            Debug.LogError(message, context);
        }

        // ReSharper disable Unity.PerformanceAnalysis
        [Conditional("UNITY_EDITOR")]
        public static void LogException(Exception exception)
        {
            // 仅在编辑器环境下调用例外接口
            Debug.LogException(exception);
        }
    }
}