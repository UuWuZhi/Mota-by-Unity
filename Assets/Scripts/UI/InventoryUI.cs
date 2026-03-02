using System.Collections.Generic;
using UnityEngine;
using VContainer;

/// <summary>
/// 动态背包面板：固定槽位数（默认36）。在 Start 时一次性创建槽位并保留，收到背包变更时仅更新各槽显示（不销毁/重建槽）。
/// 在 Inspector 中配置 slotParent(容器)、slotPrefab(包含 InventorySlot 组件)、maxSlots（可改为36）
/// </summary>
public class InventoryUI : BaseUI
{
    public Transform slotParent;    // 槽位容器
    public GameObject slotPrefab;   // 槽位预制体（包含 InventorySlot 组件）

    [Tooltip("固定槽位数量（默认36)")]
    public int maxSlots = 36;

    private List<InventorySlot> _slots = new List<InventorySlot>(); // 已创建的槽位列表
    private EventCenter _eventCenter;
    private IInventoryService _inventory;
    private ItemDatabase _itemDatabase;

    private bool _subscribed = false;       // 是否已订阅事件

    [Inject]
    public void Construct(EventCenter eventCenter, IInventoryService inventory, ItemDatabase itemDatabase = null)
    {
        _eventCenter = eventCenter;
        _inventory = inventory;
        _itemDatabase = itemDatabase;
        TrySubscribe();
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

    private void TrySubscribe()
    {
        if (_subscribed) return;
        if (_eventCenter == null) return;
        _eventCenter.OnInventoryChanged += OnInventoryChanged;
        _subscribed = true;
    }

    private void TryUnsubscribe()
    {
        if (!_subscribed) return;
        if (_eventCenter != null) _eventCenter.OnInventoryChanged -= OnInventoryChanged;
        _subscribed = false;
    }

    private void Start()
    {
        BuildFixedSlots();
        UpdateSlotsFromEntries();
    }

    private void OnInventoryChanged(object sender, InventoryChangedEventArgs args)
    {
        // 只更新显示内容，不删除槽位
        UpdateSlotsFromEntries();
    }

    private void BuildFixedSlots()
    {
        if (slotParent == null || slotPrefab == null) return;
        if (maxSlots <= 0) maxSlots = 36;
        // 如果已经存在槽位，则尽量复用它们；否则收集 slotParent 下已有的 InventorySlot
        if (_slots.Count == 0)
        {
            for (int i = 0; i < slotParent.childCount; i++)
            {
                var child = slotParent.GetChild(i);
                if (child == null) continue;
                var slot = child.GetComponent<InventorySlot>();
                if (slot != null)
                {
                    _slots.Add(slot);
                }
            }
        }

        // 如果当前槽少于目标数量，实例化缺失的槽位并添加到列表
        for (int i = _slots.Count; i < maxSlots; i++)
        {
            var go = Instantiate(slotPrefab, slotParent);
            go.name = $"InventorySlot_{i}";
            var slot = go.GetComponent<InventorySlot>();
            if (slot == null)
            {
                Debug.LogWarning("slotPrefab 缺少 InventorySlot 组件");
                Destroy(go);
                continue;
            }
            _slots.Add(slot);
        }

        // 如果当前槽多于目标数量，禁用多余槽位（不销毁，以便恢复或调试）
        if (_slots.Count > maxSlots)
        {
            for (int i = maxSlots; i < _slots.Count; i++)
            {
                var s = _slots[i];
                if (s != null && s.gameObject != null) s.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateSlotsFromEntries()
    {
        if (slotParent == null || slotPrefab == null || _inventory == null) return;

        var entries = _inventory.GetEntries();
        int count = entries != null ? entries.Count : 0;

        for (int i = 0; i < maxSlots; i++)
        {
            if (i < _slots.Count)
            {
                var slot = _slots[i];
                if (i < count)
                {
                    var e = entries[i];
                    Sprite icon = null;
                    if (_itemDatabase != null)
                    {
                        var data = _itemDatabase.Get(e.Type);
                        if (data != null) icon = data.icon;
                        else Debug.LogWarning($"ItemDatabase 中未找到类型为 {e.Type} 的物品数据");
                    }
                    slot.SetData(e.Type, e.Count, icon);
                    slot.gameObject.SetActive(true);
                }
                else
                {
                    slot.Clear();
                    // 保持槽位 GameObject 存在，但保持显示为空
                }
            }
        }

        // 若 entries 超过 maxSlots，目前仅显示前 maxSlots 条。后续可添加分页或滚动支持。
    }
}
