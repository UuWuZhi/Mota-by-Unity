using UnityEngine;
using UnityEngine.Tilemaps;
//==============================================================================//
//                                                                              //
//                                 瓦片：宝石                                    //
//                                                                              //
//==============================================================================//
[CreateAssetMenu(fileName = "GemTile", menuName = "Mota/Items/GemTile")]
public class GemTile : BaseItemTile
{
    // 宝石专属属性
    public AttributeBonus[] attributeBonuses; // 替换原来的两个数组
}