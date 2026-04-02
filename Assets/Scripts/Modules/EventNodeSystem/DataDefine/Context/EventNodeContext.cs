using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 通用节点上下文基类（轻量），用于作为所有具体上下文类型的基类。
/// EventNode.Execute 接口应接受此基类以便复用不同语义的上下文（Tile/Item 等）。
/// </summary>
public class EventNodeContext
{
    public MonoBehaviour OwnerMono; // 用于 StartCoroutine 等，通常由 Runner 注入
    public Dictionary<string, object> Vars 
        = new();                    // 临时/扩展数据
    private readonly Dictionary<Type, object> _services 
        = new();                    //按类型存储运行时服务实例

    /// <summary>
    /// 注册服务实例到类型化服务包，并尝试注入到现有的显式属性/字段以兼容旧代码。
    /// </summary>
    public void RegisterService<T>(T instance) where T : class
    {
        if (instance == null) return;
        var key = typeof(T);
        _services[key] = instance;
    }

    public void RegisterService(Type type, object instance)
    {
        if (type == null || instance == null) return;
        _services[type] = instance;
    }

    public T GetService<T>() where T : class
    {
        if (TryGetService<T>(out var svc)) return svc;
        return null;
    }

    public bool TryGetService<T>(out T service) where T : class
    {
        var key = typeof(T);
        if (_services.TryGetValue(key, out var o))
        {
            service = o as T;
            return service != null;
        }

        service = null;
        return false;
    }

    public bool HasService<T>() where T : class
    {
        return _services.ContainsKey(typeof(T));
    }

    public void Set<T>(string key, T value) where T : class
    {
        if (string.IsNullOrEmpty(key)) return;
        Vars[key] = value;
    }

    public T Get<T>(string key) where T : class
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (Vars.TryGetValue(key, out var o))
        {
            return o as T;
        }
        return null;
    }

}