using System;
using Modules.Item.DataDefine;
using Modules.Player.DataDefine;
using UnityEngine;

namespace Modules.Core.Runtime.Calculate
{
    /// <summary>
    ///     金币奖励计算器。
    /// </summary>
    public class GoldRewardCalculate
    {
        private readonly IInventoryService _inventory;

        /// <summary>
        ///     构造函数注入背包服务。
        /// </summary>
        /// <param name="inventory">背包服务。</param>
        public GoldRewardCalculate(IInventoryService inventory)
        {
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        }

        /// <summary>
        ///     计算实际获得的金币奖励。
        /// </summary>
        /// <param name="baseGold">原始金币数量，必须大于等于 0。</param>
        /// <returns>实际获得的金币数量。</returns>
        public int CalculateGoldReward(int baseGold)
        {
            int goldReward;

            try
            {
                if (baseGold <= 0)
                    goldReward = 0;
                else if (_inventory != null && _inventory.HasItem(ItemType.LuckyGold))
                    goldReward = baseGold * 2;
                else
                    goldReward = baseGold;
            }
            catch (Exception ex)
            {
                goldReward = baseGold;
                Debug.LogWarning($"[GoldRewardCalculate]:{ex}");
            }

            return goldReward;
        }
    }
}