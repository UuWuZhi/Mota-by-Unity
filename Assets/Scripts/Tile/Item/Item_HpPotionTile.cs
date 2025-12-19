using UnityEngine;
using UnityEngine.Tilemaps;
//==============================================================================//
//                                                                              //
//                                 瓦片：血瓶                                    //
//                                                                              //
//==============================================================================//
[CreateAssetMenu(fileName = "HPPotionTile", menuName = "Mota/Items/HPPotionTile")]
public class HPPotionTile : BaseItemTile
{
    // 可扩展血瓶专属属性（比如恢复量，也可复用ItemConst）
    public int HPRecover; 
}