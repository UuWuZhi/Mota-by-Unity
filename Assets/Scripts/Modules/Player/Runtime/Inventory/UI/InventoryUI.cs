using System.Collections.Generic;
using Modules.Core.Runtime;
using Modules.EventSystem.DataDefine.EventArgs;
using Modules.Player.DataDefine;
using Modules.UI.Runtime;
using UnityEngine;
using VContainer;

namespace Modules.Player.Runtime.Inventory.UI
{
    /// <summary>
    ///     动态背包面板：固定槽位数（默认36）。在 Start 时一次性创建槽位并保留，收到背包变更时仅更新各槽显示（不销毁/重建槽）。
    ///     在 Inspector 中配置 slotParent(容器)、slotPrefab(包含 InventorySlot 组件)、maxSlots（可改为36）
    /// </summary>
    public class InventoryUI : BaseUI
    {
        private const int MaxSlots = 36; // 固定槽位数量
        public Transform slotParent; // 槽位容器
        public GameObject slotPrefab; // 槽位预制体（包含 InventorySlot 组件）
        private readonly List<InventorySlot> _slots = new(); // 已创建的槽位列表

        private IInventoryService _inventory;
        private ItemDatabase _itemDatabase;

        private bool _subscribed; // 是否已订阅事件

        private void Start()
        {
            BuildFixedSlots();
            UpdateSlotsFromEntries();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            TrySubscribe();
            UpdateSlotsFromEntries();
        }

        protected override void OnDestroy()
        {
            TryUnsubscribe();
            base.OnDestroy();
        }

        [Inject]
        public void Construct(IInventoryService inventory, ItemDatabase itemDatabase = null)
        {
            _inventory = inventory;
            _itemDatabase = itemDatabase;
            TrySubscribe();
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            if (_inventory == null) return;
            _inventory.InventoryChanged += OnInventoryChanged;
            _subscribed = true;
        }

        private void TryUnsubscribe()
        {
            if (!_subscribed) return;
            if (_inventory != null) _inventory.InventoryChanged -= OnInventoryChanged;
            _subscribed = false;
        }

        private void OnInventoryChanged(object sender, InventoryChangedEventArgs args)
        {
            // 只更新显示内容，不删除槽位
            UpdateSlotsFromEntries();
        }

        private void BuildFixedSlots()
        {
            if (!slotParent || !slotPrefab) return;
            // 收集已有槽位
            if (_slots.Count != 0) return;
            for (var i = 0; i < slotParent.childCount; i++)
            {
                var child = slotParent.GetChild(i);
                if (!child) continue;
                var slot = child.GetComponent<InventorySlot>();
                if (slot) _slots.Add(slot);
            }
        }

        private void UpdateSlotsFromEntries()
        {
            if (!slotParent || !slotPrefab || _inventory == null) return;

            var entries = _inventory.GetEntries();
            var count = entries?.Count ?? 0;

            for (var i = 0; i < MaxSlots; i++)
            {
                if (i >= _slots.Count) continue;
                var slot = _slots[i];
                if (i < count)
                {
                    if (entries != null)
                    {
                        var e = entries[i];
                        Sprite icon = null;
                        if (_itemDatabase)
                        {
                            var data = _itemDatabase.Get(e.type);
                            if (data) icon = data.icon;
                            else DebugEditor.LogWarning($"ItemDatabase 中未找到类型为 {e.type} 的物品数据");
                        }

                        slot.SetData(e.type, e.count, icon);
                    }

                    slot.gameObject.SetActive(true);
                }
                else
                {
                    slot.Clear();
                    // 保持槽位 GameObject 存在，但保持显示为空
                }
            }

            // 若 entries 超过 maxSlots，目前仅显示前 maxSlots 条。后续可添加分页或滚动支持。
        }
    }
}