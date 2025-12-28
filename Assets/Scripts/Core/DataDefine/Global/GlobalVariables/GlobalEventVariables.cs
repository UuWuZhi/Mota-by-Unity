using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

/// <summary>
/// 仅作为 Inspector 桥接器：把 Inspector 的配置同步到容器管理的 GlobalEventVariablesService
/// 在迁移期仍保留静态 Instance 以兼容旧代码，但首选使用 IGlobalEventVariables 注入
/// </summary>
public class GlobalEventVariables : MonoBehaviour
{
    public static GlobalEventVariables Instance { get; private set; }

    [System.Serializable]
    public class Entry
    {
        public GlobalEventKey Key;
        public GlobalEventVariablesService.ValueType Type;
        public int IntValue;
        public float FloatValue;
        public string StringValue;
        public bool BoolValue;
    }

    [Header("Inspector Variables (同步到运行时字典)")]
    public List<Entry> inspectorEntries = new List<Entry>();

    private IGlobalEventVariables _service;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    [Inject]
    public void Construct(IGlobalEventVariables service)
    {
        _service = service;
        SyncFromInspectorToService();
    }

    private void SyncFromInspectorToService()
    {
        if (_service == null) return;
        foreach (var e in inspectorEntries)
        {
            // No null/empty check needed for enum; skip default values if necessary
            switch (e.Type)
            {
                case GlobalEventVariablesService.ValueType.Int:
                    _service.SetInt(e.Key, e.IntValue);
                    break;
                case GlobalEventVariablesService.ValueType.Float:
                    _service.SetFloat(e.Key, e.FloatValue);
                    break;
                case GlobalEventVariablesService.ValueType.String:
                    _service.SetString(e.Key, e.StringValue ?? string.Empty);
                    break;
                case GlobalEventVariablesService.ValueType.Bool:
                    _service.SetBool(e.Key, e.BoolValue);
                    break;
            }
        }
    }
}