using System.Collections.Generic;
using UnityEngine;

// 瓦片：障碍物，继承自 BaseTile
[CreateAssetMenu(fileName = "ObstacleTile", menuName = "Tile/ObstacleTile")]
public class ObstacleTile : BaseTile
{
    [Tooltip("是否可破坏（可被攻击或道具破坏）")]
    public bool IsBreakable;

    [Tooltip("可用于破坏此障碍的道具类型列表（ItemType）")]
    public List<ItemType> BreakableBy = new List<ItemType>();
}
