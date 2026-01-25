// 玩家背包管理器
using UnityEngine;
using System.Collections.Generic;
using System;
using VContainer;

public class PlayerInventory : MonoBehaviour, IInventoryService
{
    private readonly List<InventoryEntry> entries = new List<InventoryEntry>();

    private EventCenter _eventCenter;
    [Inject]
    public void Inject(EventCenter eventCenter)
    {
        _eventCenter = eventCenter;
    }
    //==============================================================================//
    //                                                                              //
    //                                 道具操作                                     //
    //                                                                              //
    //==============================================================================//
    #region 道具操作
    /// <summary>
    /// 初始化背包（清空所有条目）
    /// </summary>
    public void InitItemCounts()
    {
        entries.Clear();
        // 通过 EventCenter 广播全量更新
        _eventCenter.TriggerInventoryChanged(new InventoryChangedEventArgs(ItemType.All));
    }
    /// <summary>
    /// 添加道具（简单实现：在末尾追加新条目）
    /// </summary>
    public void AddItem(ItemType type, int count = 1)
    {
        if (type == ItemType.None || count <= 0) return;

        entries.Add(new InventoryEntry(type, count));
        _eventCenter.TriggerInventoryChanged(new InventoryChangedEventArgs(type));
        Debug.Log($"获得{type}×{count}！当前条目数：{entries.Count}");
    }
    /// <summary>
    /// 移除道具（返回是否成功）
    /// 从头到尾遍历找到同类型条目，逐个扣减，条目计数为0则移除
    /// </summary>
    public bool RemoveItem(ItemType type, int count = 1)
    {
        if (type == ItemType.None || count <= 0) return false;

        int remaining = count;
        for (int i = 0; i < entries.Count && remaining > 0; )
        {
            var e = entries[i];
            if (e.Type != type) { i++; continue; }

            if (e.Count > remaining)
            {
                e.Count -= remaining;
                remaining = 0;
            }
            else
            {
                remaining -= e.Count;
                // remove this entry
                entries.RemoveAt(i);
                continue; // 不递增 i，因为移除后当前位置被下一个填充
            }
            i++;
        }

        if (remaining > 0)
        {
            // not enough items: 失败（也可选择回滚，当前实现为不回滚，返回 false）
            return false;
        }

        // 广播全量更新
        _eventCenter.TriggerInventoryChanged(new InventoryChangedEventArgs(type));
        return true;
    }
    /// <summary>
    /// 检查是否拥有指定数量的道具
    /// </summary>
    public bool HasItem(ItemType type, int count = 1)
    {
        int total = GetItemCount(type);
        return total >= count;
    }
    /// <summary>
    /// 获取道具总数量（会遍历所有条目累加）
    /// </summary>
    public int GetItemCount(ItemType type)
    {
        int total = 0;
        foreach (var e in entries) if (e.Type == type) total += e.Count;
        return total;
    }

    /// <summary>
    /// 返回当前条目列表的只读视图，供 UI 使用
    /// </summary>
    public IReadOnlyList<InventoryEntry> GetEntries()
    {
        return entries.AsReadOnly();
    }
    #endregion

}