using System.Collections.Generic;
using System.Linq;
using Modules.Item.DataDefine;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Data/Item/ItemDatabase", order = 4)]
public class ItemDatabase : ScriptableObject
{
    public List<ItemData> items = new(); //物品数据列表
    private Dictionary<ItemType, ItemData> _map; //物品数据映射表

    private void OnEnable()
    {
        BuildMap();
    }

    public void BuildMap()
    {
        _map = new Dictionary<ItemType, ItemData>();
        foreach (var it in items.Where(it => it))
        {
            if (!_map.TryAdd(it.type, it))
            {
                Debug.LogWarning($"ItemDatabase:物品ID重复！：{it.type}");
                continue;
            }

            Debug.Log($"ItemDatabase:加载物品数据：{it.type}");
        }
    }


    public ItemData Get(ItemType type)
    {
        if (_map == null) BuildMap();
        System.Diagnostics.Debug.Assert(_map != null, nameof(_map) + " != null");
        _map.TryGetValue(type, out var data);
        return data;
    }
}