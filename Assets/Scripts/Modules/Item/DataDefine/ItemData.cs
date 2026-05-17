using Modules.EventNodeSystem.DataDefine;
using Modules.Item.DataDefine;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Data/Item/ItemData", order = 3)]
public class ItemData : ScriptableObject
{
    public enum ItemUseMode
    {
        Unusable,
        Consumable,
        Reusable
    }

    public ItemType type; // 物品类型ID（内部唯一标识）
    public string displayName; // 物品显示名称
    public Sprite icon; // 物品图标
    [TextArea] public string description; // 物品描述

    public ItemUseMode useMode // 使用模式
        = ItemUseMode.Unusable; // 不可用 / 消耗性 / 可重复使用

    /// <summary>
    ///     物品使用时执行的事件序列。
    /// </summary>
    public EventSequence useSequence = new();
}