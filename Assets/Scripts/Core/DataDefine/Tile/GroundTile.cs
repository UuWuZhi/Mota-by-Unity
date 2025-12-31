using UnityEngine;
using UnityEngine.Tilemaps;

// 瓦片：地面，继承自 BaseTile
[CreateAssetMenu(fileName = "GroundTile", menuName = "Mota/GroundTile")]
public class GroundTile : BaseTile
{
    [Tooltip("高度，用于判定地形高度或可通过性")]
    public int Height;
}
