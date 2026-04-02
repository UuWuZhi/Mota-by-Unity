using System;
using UnityEngine;


public class GoldRewardCaculate
{
    private readonly IInventoryService _inventory;

    // Constructor injection via VContainer
    public GoldRewardCaculate(IInventoryService inventory)
    {
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
    }

    /// <summary>
    /// 计算怪物掉落金币收益：如果玩家拥有 Lucky_Gold 道具，则收益翻倍。
    /// </summary>
    /// <param name="baseGold">怪物原始掉落金币数量（>=0）</param>
    /// <returns>实际获得的金币数量</returns>
    public int CalculateGoldReward(int baseGold)
    {
        int GoldReward;

        try
        {
            if (baseGold <= 0)
            {
                GoldReward = 0;
            }
            // 检查是否拥有幸运金币（Lucky_Gold）
            else if (_inventory != null && _inventory.HasItem(ItemType.Lucky_Gold))
            {
                GoldReward = baseGold * 2;
            }
            else
            {
                GoldReward = baseGold;
            }
        }
        catch (Exception)
        {
            // 如果查询失败，遵循保守策略：返回原始值
            GoldReward = baseGold;
            Debug.LogWarning("未能获取玩家道具信息，金币奖励按原始值计算。");
        }

        return GoldReward;
    }
}
