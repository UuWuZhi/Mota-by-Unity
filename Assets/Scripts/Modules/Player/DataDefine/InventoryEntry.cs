using System;
using Modules.Item.DataDefine;
using UnityEngine.Serialization;

namespace Modules.Player.DataDefine
{
    [Serializable]
    public class InventoryEntry
    {
        [FormerlySerializedAs("Type")] public ItemType type;
        [FormerlySerializedAs("Count")] public int count;

        public InventoryEntry()
        {
        }

        public InventoryEntry(ItemType type, int count)
        {
            this.type = type;
            this.count = count;
        }
    }
}