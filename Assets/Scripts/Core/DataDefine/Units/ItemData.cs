using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Data/Item/ItemData", order = 3)]
public class ItemData : ScriptableObject
{
    public ItemType type;       //物品类型ID（内部唯一标识）
    public string displayName;  //物品显示名称
    public Sprite icon;         //物品图标
    [TextArea]
    public string description;  //物品描述

    // 引用该道具的运行时使用逻辑（ScriptableObject，继承 BaseItem）
    public BaseItem behavior;

    // 使用模式：不可用 / 消耗性 / 可重复使用
    public ItemUseMode useMode = ItemUseMode.Unusable;
    // maxStack omitted for now
}

public enum ItemUseMode
{
    Unusable,
    Consumable,
    Reusable,
}
