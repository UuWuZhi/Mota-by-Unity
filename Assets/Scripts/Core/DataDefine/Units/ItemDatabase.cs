using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Data/Item/ItemDatabase", order = 4)]
public class ItemDatabase : ScriptableObject
{

    public List<ItemData> items = new List<ItemData>(); //物品数据列表
    private Dictionary<ItemType, ItemData> _map;        //物品数据映射表

    private void OnEnable()
    {
        BuildMap();
    }

    public void BuildMap()
    {
        _map = new Dictionary<ItemType, ItemData>();
        foreach (var it in items)
        {
            if (it == null) continue;
            if (_map.ContainsKey(it.type))
            {
                Debug.LogWarning($"ItemDatabase:物品ID重复！：{it.type}");
                continue;
            }
            _map[it.type] = it;
            Debug.Log($"ItemDatabase:加载物品数据：{it.type}");
        }
    }



    public ItemData Get(ItemType type)
    {
        if (_map == null) BuildMap();
        _map.TryGetValue(type, out var data);
        return data;
    }
}
