using UnityEngine;
using System.Collections.Generic;
using System; // 引入Action需要的命名空间

/// <summary>
/// 玩家背包管理器：仅负责道具数量管理，属性修改通过Player_Attribute完成
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance;

    // 核心：存储所有道具的数量（Key=道具类型，Value=数量）
    private Dictionary<ItemType, int> itemCounts = new Dictionary<ItemType, int>();
    //物品数量更改
    public event Action<ItemType> OnItemChanged;
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

    #region 通用道具操作
    /// <summary>
    /// 添加道具（通用）
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
    /// 获取道具数量（供UI调用）
    /// </summary>
    public int GetItemCount(ItemType type)
    {
        return itemCounts.TryGetValue(type, out int count) ? count : 0;
    }
    #endregion

}