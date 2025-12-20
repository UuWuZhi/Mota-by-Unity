// 玩家背包管理器
using UnityEngine;
using System.Collections.Generic;
using System;
public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance;

    private Dictionary<ItemType, int> itemCounts = new Dictionary<ItemType, int>();
    public event Action<ItemType> OnItemChanged;
    //==============================================================================//
    //                                                                              //
    //                                 生命周期                                     //
    //                                                                              //
    //==============================================================================//
    #region 生命周期
    private void Awake()
    {
        // 单例初始化
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    #endregion
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
        OnItemChanged?.Invoke(ItemType.All); //初始化完更新一下UI
    }
    /// <summary>
    /// 添加道具
    /// </summary>
    public void AddItem(ItemType type, int count = 1)
    {
        if (!itemCounts.ContainsKey(type) || type == ItemType.None || count <= 0) return;
        
        itemCounts[type] += count;
        OnItemChanged?.Invoke(type); //更新对应UI
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
        OnItemChanged?.Invoke(type);
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