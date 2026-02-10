using System.Collections;
using UnityEngine;

/// <summary>
/// Lightweight coroutine host. Attach to a GameObject in scene (e.g. GameRoot) and register via DI.
/// Provides StartCoroutine wrapper for non-Mono services.
/// </summary>
public class CoroutineRunner : MonoBehaviour
{
    public Coroutine Run(IEnumerator routine) => StartCoroutine(routine);
    public void StopRunning(Coroutine c) { if (c != null) StopCoroutine(c); }
}
