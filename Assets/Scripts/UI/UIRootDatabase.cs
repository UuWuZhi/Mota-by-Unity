using System.Collections.Generic;
using UnityEngine;

// ScriptableObject：维护 UI Root 与 枚举类型的映射（作为单一 DB）
[CreateAssetMenu(fileName = "UIRootDatabase", menuName = "UI/UI Root Database")]
public class UIRootDatabase : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public UIRootType Type;
        public GameObject Root; // 推荐引用 UI 根 prefab 或场景实例（prefab 推荐）
    }

    public List<Entry> Entries = new List<Entry>();

    public GameObject GetRoot(UIRootType type)
    {
        if (type == UIRootType.None) return null;
        for (int i = 0; i < Entries.Count; i++) if (Entries[i].Type == type) return Entries[i].Root;
        return null;
    }

    public IEnumerable<Entry> GetAllEntries() => Entries;
}
