using System.Collections.Generic;
using UnityEngine;

namespace Modules.Core.DataDefine.Units
{
    /// <summary>
    ///     物品序列组件
    /// </summary>
    public class AttributeUnit : MonoBehaviour
    {
        public List<AttributeBonus> attributeBonuses = new();
    }
}