// 玩家背包管理器

using System;
using System.Collections.Generic;
using System.Linq;
using Modules.Core.Runtime;
using Modules.EventSystem.DataDefine.EventArgs;
using Modules.Item.DataDefine;
using Modules.Player.DataDefine;
using UnityEngine;

public class PlayerInventory : MonoBehaviour, IInventoryService
{
    private readonly List<InventoryEntry> _entries = new();

    public event EventHandler<InventoryChangedEventArgs> InventoryChanged;

    #region 道具操作

    /// <summary>
    ///     初始化背包（清空所有条目）
    /// </summary>
    public void InitItemCounts()
    {
        _entries.Clear();
        // 通过 EventCenter 广播全量更新
        InventoryChanged?.Invoke(this, new InventoryChangedEventArgs(ItemType.All));
    }

    /// <summary>
    ///     添加道具（简单实现：在末尾追加新条目）
    /// </summary>
    public void AddItem(ItemType type, int count = 1)
    {
        if (type == ItemType.None || count <= 0) return;

        _entries.Add(new InventoryEntry(type, count));
        InventoryChanged?.Invoke(this, new InventoryChangedEventArgs(type));
        DebugEditor.Log($"获得{type}×{count}！当前条目数：{_entries.Count}");
    }

    /// <summary>
    ///     移除道具（返回是否成功）
    ///     从头到尾遍历找到同类型条目，逐个扣减，条目计数为0则移除
    /// </summary>
    public bool RemoveItem(ItemType type, int count = 1)
    {
        if (type == ItemType.None || count <= 0) return false;

        var remaining = count;
        for (var i = 0; i < _entries.Count && remaining > 0;)
        {
            var e = _entries[i];
            if (e.type != type)
            {
                i++;
                continue;
            }

            if (e.count > remaining)
            {
                e.count -= remaining;
                remaining = 0;
            }
            else
            {
                remaining -= e.count;
                // remove this entry
                _entries.RemoveAt(i);
                continue; // 不递增 i，因为移除后当前位置被下一个填充
            }

            i++;
        }

        if (remaining > 0)
            // not enough items: 失败（也可选择回滚，当前实现为不回滚，返回 false）
            return false;

        // 广播全量更新
        InventoryChanged?.Invoke(this, new InventoryChangedEventArgs(type));
        return true;
    }

    /// <summary>
    ///     检查是否拥有指定数量的道具
    /// </summary>
    public bool HasItem(ItemType type, int count = 1)
    {
        var total = GetItemCount(type);
        return total >= count;
    }

    /// <summary>
    ///     获取道具总数量（会遍历所有条目累加）
    /// </summary>
    public int GetItemCount(ItemType type)
    {
        return _entries.Where(e => e.type == type).Sum(e => e.count);
    }

    /// <summary>
    ///     返回当前条目列表的只读视图，供 UI 使用
    /// </summary>
    public IReadOnlyList<InventoryEntry> GetEntries()
    {
        return _entries.AsReadOnly();
    }

    #endregion
}