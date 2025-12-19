using UnityEngine;
using UnityEngine.Tilemaps;
//==============================================================================//
//                                                                              //
//                                 瓦片：基础道具                                //
//                                                                              //
//==============================================================================//
// 所有道具瓦片的基类
[CreateAssetMenu(fileName = "BaseItemTile", menuName = "Mota/Items/BaseItemTile")]
public class BaseItemTile : EventTile
{
    [Header("通用道具配置")]
    public ItemType itemType;  // 道具类型
    public string itemName;    // 道具名称
    public Sprite icon;        // 道具图标（用于UI）
    [SerializeField] private int itemId;
    public int ItemId => itemId;
    [TextArea] public string desc; // 道具描述
}