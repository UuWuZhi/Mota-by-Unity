using UnityEngine;
using UnityEngine.Tilemaps;
//==============================================================================//
//                                                                              //
//                                 瓦片：楼梯                                    //
//                                                                              //
//==============================================================================//
// 楼梯瓦片：继承EventTile，属于事件层Tile
[CreateAssetMenu(fileName = "StairTile", menuName = "Mota/Tile/StairTile")]
public class StairTile : EventTile
{
    [Header("楼梯核心配置")]
    public StairType stairType; // 楼梯类型
    public int targetLayerId; // 目标楼层ID（仅Custom生效）
    [Tooltip("是否使用目标楼层的默认SpawnPoint（优先选这个）")]
    public bool useLayerSpawnPoint = true; 
    [Tooltip("手动指定目标位置（仅useLayerSpawnPoint=false时生效）")]
    public Vector2 customTargetPos; 
}