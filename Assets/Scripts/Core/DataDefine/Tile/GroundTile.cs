using UnityEngine;
using UnityEngine.Tilemaps;

// 瓦片：地面，继承自 BaseTile
[CreateAssetMenu(fileName = "GroundTile", menuName = "Tile/GroundTile")]
public class GroundTile : BaseTile
{
    [Tooltip("高度，用于更改玩家所处高度")]
    public int HeightChange;
}
