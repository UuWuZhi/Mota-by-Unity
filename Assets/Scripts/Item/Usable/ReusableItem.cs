using UnityEngine;

[CreateAssetMenu(fileName = "ReusableItem", menuName = "Data/Item/Usable/ReusableItem", order = 11)]
public class ReusableItem : BaseItem
{
    // 示例：可配置冷却或无限使用
    public string actionName;

    public override bool Use()
    {
        Debug.Log($"ReusableItem.Use: 使用可重复道具 {name}, action {actionName}");
        // 返回 false 表示未被消耗
        return false;
    }
}
