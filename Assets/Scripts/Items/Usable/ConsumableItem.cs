using UnityEngine;

[CreateAssetMenu(fileName = "ConsumableItem", menuName = "Data/Item/Usable/ConsumableItem", order = 10)]
public class ConsumableItem : BaseItem
{
    public override bool Use()
    {
        return true;
    }
}
