using System.Collections.Generic;
using Modules.Item.DataDefine;
using UnityEngine;

namespace Modules.Core.DataDefine.Units
{
    /// <summary>
    ///     物品序列组件
    /// </summary>
    public class ItemUnit : MonoBehaviour
    {
        public List<ItemBonus> itemBonuses = new();
    }
}