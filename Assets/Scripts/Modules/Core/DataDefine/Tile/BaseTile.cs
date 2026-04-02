using UnityEngine;
using UnityEngine.Tilemaps;
//==============================================================================//
//                                                                              //
//                                 瓦片：基础                                    //
//                                                                              //
//==============================================================================//
[CreateAssetMenu(fileName = "BaseTile", menuName = "Tile/BaseTile")]
public class BaseTile : Tile
{
    [Tooltip("标记当前瓦片是物品，门，敌人还是自定义类型")]
    public GridType tileType; //GridType
}