using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// ScriptableObject：维护 UI Root 与 枚举类型的映射（作为单一 DB）
namespace Modules.UI.DataDefine
{
    [CreateAssetMenu(fileName = "UIRootDatabase", menuName = "UI/UI Root Database")]
    public class UIRootDatabase : ScriptableObject
    {
        [FormerlySerializedAs("Entries")] public List<Entry> entries = new();

        public GameObject GetRoot(UIRootType type)
        {
            if (type == UIRootType.None) return null;
            for (var i = 0; i < entries.Count; i++)
                if (entries[i].type == type)
                    return entries[i].root;
            return null;
        }

        public IEnumerable<Entry> GetAllEntries()
        {
            return entries;
        }

        [Serializable]
        public struct Entry
        {
            [FormerlySerializedAs("Type")] public UIRootType type;
            [FormerlySerializedAs("Root")] public GameObject root; // 推荐引用 UI 根 prefab 或场景实例（prefab 推荐）
        }
    }
}