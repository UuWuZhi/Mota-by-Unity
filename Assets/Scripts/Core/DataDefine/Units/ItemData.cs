using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Data/Item/ItemData", order = 3)]
public class ItemData : ScriptableObject
{
    public ItemType type;       //物品类型ID（内部唯一标识）
    public string displayName;  //物品显示名称
    public Sprite icon;         //物品图标
    [TextArea]
    public string description;  //物品描述
    // maxStack omitted for now
}
