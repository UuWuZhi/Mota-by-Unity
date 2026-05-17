using UnityEngine;
using UnityEngine.Serialization;

// 瓦片：地面，继承自 BaseTile
namespace Modules.Map.DataDefine.Tile
{
    [CreateAssetMenu(fileName = "GroundTile", menuName = "Tile/GroundTile")]
    public class GroundTile : BaseTile
    {
        [FormerlySerializedAs("HeightChange")] [Tooltip("高度，用于更改玩家所处高度")]
        public int heightChange;
    }
}