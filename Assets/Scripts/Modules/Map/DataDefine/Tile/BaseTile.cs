using UnityEngine;

namespace Modules.Map.DataDefine.Tile
{
    [CreateAssetMenu(fileName = "BaseTile", menuName = "Tile/BaseTile")]
    public class BaseTile : UnityEngine.Tilemaps.Tile
    {
        [Tooltip("标记当前瓦片是物品，门，敌人还是自定义类型")] public GridType tileType; //GridType
    }
}