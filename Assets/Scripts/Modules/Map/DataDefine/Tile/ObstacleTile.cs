using System.Collections.Generic;
using Modules.Item.DataDefine;
using UnityEngine;
using UnityEngine.Serialization;

// 瓦片：障碍物，继承自 BaseTile
namespace Modules.Map.DataDefine.Tile
{
    [CreateAssetMenu(fileName = "ObstacleTile", menuName = "Tile/ObstacleTile")]
    public class ObstacleTile : BaseTile
    {
        [FormerlySerializedAs("IsBreakable")] [Tooltip("是否可破坏（可被攻击或道具破坏）")]
        public bool isBreakable;

        [FormerlySerializedAs("BreakableBy")] [Tooltip("可用于破坏此障碍的道具类型列表（ItemType）")]
        public List<ItemType> breakableBy = new();
    }
}