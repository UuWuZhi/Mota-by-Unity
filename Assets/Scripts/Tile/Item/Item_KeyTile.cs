using UnityEngine;
using UnityEngine.Tilemaps;
//==============================================================================//
//                                                                              //
//                                 瓦片：钥匙                                    //
//                                                                              //
//==============================================================================//
// 钥匙瓦片（继承基类）
[CreateAssetMenu(fileName = "KeyTile", menuName = "Mota/Items/KeyTile")]
public class KeyTile : BaseItemTile
{
    // 钥匙无需额外属性，直接复用基类的itemType（Key_Red/Blue/Yellow）
}