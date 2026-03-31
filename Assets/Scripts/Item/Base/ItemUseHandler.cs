using System;
using UnityEngine;
using VContainer;
using System.Collections.Generic;

/// <summary>
/// 物品使用入口：构造 ItemEventContext 并用 IEventRunner.RunActions 执行节点列表。
/// 约定：节点执行后应在 ctx.Vars["use_succeeded"] 写入 bool，或节点自行调用 IInventoryService 处理消耗。
/// </summary>
public class ItemUseHandler
{
    private readonly IEventRunner _runner;
    private readonly IInventoryService _inventory;

    [Inject]
    public ItemUseHandler(IEventRunner runner, IInventoryService inventory)
    {
        _runner = runner;
        _inventory = inventory;
    }

    public void UseItem(ItemData item, GameObject caller = null, Vector3? worldPos = null, int slotIndex = -1, int count = 1, Action onComplete = null)
    {
        if (item == null || item.useNodes == null || item.useNodes.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        // 使用项目中已有的 ItemEventContext（位于 EventNodeSystem/Context）
        var ctx = new ItemEventContext();
        // 通过通用 Vars/Set 注入调用信息（ItemEventContext 只保留 ItemData 与 Consumed 标记）
        ctx.ItemData = item;
        if (caller != null) ctx.Set("caller", caller);
        if (worldPos.HasValue) ctx.Vars["worldPos"] = worldPos.Value;
        ctx.Set("slotIndex", slotIndex >= 0 ? (object)slotIndex : null);
        ctx.Set("count", count > 0 ? (object)count : null);

        // Runner 会根据节点的 GetRequiredServices 自动注册已知服务到 ctx
        _runner.RunActions(item.useNodes, ctx, () =>
        {
            // 约定：节点在成功时应调用 ItemEventContext.MarkUseSucceeded(); 默认为未成功
            bool succeeded = false;
            if (ctx is ItemEventContext iec && iec.UseSucceeded) succeeded = true;

            if (succeeded && item.useMode == ItemData.ItemUseMode.Consumable)
            {
                // 如果节点没有自行消费，则由此处统一消费
                _inventory.RemoveItem(item.type, 1);
            }
            onComplete?.Invoke();
        });
    }
}
