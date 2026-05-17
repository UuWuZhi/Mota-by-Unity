using System;
using Modules.EventNodeSystem.DataDefine.Context;
using Modules.EventNodeSystem.DataDefine.Runner;
using Modules.Player.DataDefine;
using UnityEngine;
using VContainer;

namespace Modules.Item.Runtime
{
    /// <summary>
    ///     物品使用入口：构造 ItemEventContext 并用 IEventRunner.StartSequence 执行事件序列。
    ///     约定：节点执行后应调用 ItemEventContext.MarkUseSucceeded()，或节点自行调用 IInventoryService 处理消耗。
    /// </summary>
    public class ItemUseHandler
    {
        private readonly IInventoryService _inventory;
        private readonly IEventRunner _runner;

        [Inject]
        public ItemUseHandler(IEventRunner runner, IInventoryService inventory)
        {
            _runner = runner;
            _inventory = inventory;
        }

        public void UseItem(ItemData item, GameObject caller = null, Vector3? worldPos = null, int slotIndex = -1,
            int count = 1, Action onComplete = null)
        {
            if (!item || item.useSequence?.commands == null || item.useSequence.commands.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            // 使用项目中已有的 ItemEventContext（位于 EventNodeSystem/Context）
            var ctx = new ItemEventContext
            {
                // 通过通用 Vars/Set 注入调用信息（ItemEventContext 只保留 ItemData 与 Consumed 标记）
                ItemData = item
            };
            if (caller) ctx.Set(ContextVarKey.Caller, caller);
            if (worldPos.HasValue) ctx.Set(ContextVarKey.WorldPos, worldPos.Value);
            if (slotIndex >= 0) ctx.Set(ContextVarKey.SlotIndex, slotIndex);
            if (count > 0) ctx.Set(ContextVarKey.UseCount, count);

            // Runner 会根据节点的 GetRequiredServices 自动注册已知服务到 ctx
            _runner.StartSequence(item.useSequence, ctx, () =>
            {
                // 约定：节点在成功时应调用 ItemEventContext.MarkUseSucceeded(); 默认为未成功
                var succeeded = ctx is { UseSucceeded: true };

                if (succeeded && item.useMode == ItemData.ItemUseMode.Consumable)
                    // 如果节点没有自行消费，则由此处统一消费
                    _inventory.RemoveItem(item.type);
                onComplete?.Invoke();
            });
        }
    }
}