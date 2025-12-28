// 玩家背包管理器
using UnityEngine;
using System.Collections.Generic;
using System;
using VContainer;

public class PlayerInventory : MonoBehaviour
{
    private Dictionary<ItemType, int> itemCounts = new Dictionary<ItemType, int>();

    private EventCenter _eventCenter;
    [Inject]
    public void Construct(EventCenter eventCenter)
    {
        _eventCenter = eventCenter;
        DontDestroyOnLoad(this);
    }

    //==============================================================================//
    //                                                                              //
    //                                 道具操作                                     //
    //                                                                              //
    //==============================================================================//
    #region 道具操作
    /// <summary>
    /// 初始化所有道具数量为0，由GameInitializer调用
    /// </summary>
    public void InitItemCounts()
    {
        foreach (ItemType type in System.Enum.GetValues(typeof(ItemType)))
        {
            if (type != ItemType.None)
                itemCounts[type] = 0;
        }
        // 通过 EventCenter 广播全量更新
        _eventCenter.TriggerInventoryChanged(new InventoryChangedEventArgs(ItemType.All));
    }
    /// <summary>
    /// 添加道具
    /// </summary>
    public void AddItem(ItemType type, int count = 1)
    {
        if (!itemCounts.ContainsKey(type) || type == ItemType.None || count <= 0) return;
        
        itemCounts[type] += count;
        // 使用 EventCenter 广播
        _eventCenter.TriggerInventoryChanged(new InventoryChangedEventArgs(type, count));
        Debug.Log($"获得{type}×{count}！当前数量：{itemCounts[type]}");
    }
    /// <summary>
    /// 移除道具（返回是否成功）
    /// </summary>
    public bool RemoveItem(ItemType type, int count = 1)
    {
        if (!itemCounts.ContainsKey(type) || itemCounts[type] < count || type == ItemType.None || count <= 0)
            return false;
        
        itemCounts[type] -= count;
        _eventCenter.TriggerInventoryChanged(new InventoryChangedEventArgs(type, -count));
        return true;
    }
    /// <summary>
    /// 检查是否拥有指定数量的道具
    /// </summary>
    public bool HasItem(ItemType type, int count = 1)
    {
        return itemCounts.ContainsKey(type) && itemCounts[type] >= count;
    }
    /// <summary>
    /// 获取道具数量
    /// </summary>
    public int GetItemCount(ItemType type)
    {
        return itemCounts.TryGetValue(type, out int count) ? count : 0;
    }
    #endregion

}