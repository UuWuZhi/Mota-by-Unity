using System.Collections;
using UnityEngine;

namespace Modules.EventNodeSystem.Runtime.Runner
{
    /// <summary>
    ///     作为 MonoBehaviour 容器，用于在 Unity 主线程上启动和停止协程，便于从非 MonoBehaviour 上下文运行 IEnumerator。
    /// </summary>
    /// <remarks>
    ///     必须附加到一个活动的 GameObject。StartCoroutine 和 StopCoroutine 仅在 Unity 的主线程上下文中有效。StopRunning 在传入 null
    ///     时安全无操作。
    /// </remarks>
    public class CoroutineRunner : MonoBehaviour
    {
        public Coroutine Run(IEnumerator routine)
        {
            return StartCoroutine(routine);
        }

        public void StopRunning(Coroutine c)
        {
            if (c != null) StopCoroutine(c);
        }
    }
}