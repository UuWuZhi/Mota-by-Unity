using TMPro;
using UnityEngine;
using VContainer;

// 将属性与物品 UI 合并为单个主 UI 类，并继承自 BaseUI
public class UIMain : BaseUI
{
    [Header("属性 UI")]
    [SerializeField] private TextMeshProUGUI textHp;
    [SerializeField] private TextMeshProUGUI textAttack;
    [SerializeField] private TextMeshProUGUI textDefense;
    [SerializeField] private TextMeshProUGUI textGold;

    [Header("物品 UI")]
    [SerializeField] private TextMeshProUGUI textYellowkey;
    [SerializeField] private TextMeshProUGUI textBluekey;
    [SerializeField] private TextMeshProUGUI textRedkey;

    private PlayerAttribute _playerAttribute;
    private EventCenter _eventCenter;
    private IInventoryService _inventoryService;

    protected override bool ShowOnAwake => true;
    private bool _subscribed = false;

    // VContainer 注入
    [Inject]
    public void Construct(PlayerAttribute playerAttribute, EventCenter eventCenter, IInventoryService inventory)
    {
        _playerAttribute = playerAttribute;
        _eventCenter = eventCenter;
        _inventoryService = inventory;
    }

    private void Start()
    {
        // 初始化 UI 显示
        if (_playerAttribute != null) UpdateUI(AttributeType.All);
        if (_inventoryService != null) UpdateUI(ItemType.All);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeEventCenter();
    }

    private void OnDisable()
    {
        UnsubscribeEventCenter();
    }

    private void SubscribeEventCenter()
    {
        if (!_subscribed && _eventCenter != null)
        {
            _eventCenter.OnAttributeChanged += OnAttributeChanged;
            _eventCenter.OnInventoryChanged += OnInventoryChanged_EventCenter;
            _subscribed = true;
        }
    }

    private void UnsubscribeEventCenter()
    {
        if (_subscribed && _eventCenter != null)
        {
            _eventCenter.OnAttributeChanged -= OnAttributeChanged;
            _eventCenter.OnInventoryChanged -= OnInventoryChanged_EventCenter;
            _subscribed = false;
        }
    }

    private void OnAttributeChanged(object sender, AttributeChangedEventArgs args)
    {
        UpdateUI(args.ChangedType);
    }

    private void OnInventoryChanged_EventCenter(object sender, InventoryChangedEventArgs args)
    {
        if (args == null) return;
        UpdateUI(args.ChangedType);
    }

    // 属性 UI 更新
    private void UpdateUI(AttributeType type)
    {
        if (_playerAttribute == null) return;
        switch (type)
        {
            case AttributeType.All:
                UpdateHpUI();
                UpdateAttackUI();
                UpdateDefenseUI();
                UpdateGoldUI();
                break;
            case AttributeType.HP:
                UpdateHpUI();
                break;
            case AttributeType.Attack:
                UpdateAttackUI();
                break;
            case AttributeType.Defense:
                UpdateDefenseUI();
                break;
            case AttributeType.Gold:
                UpdateGoldUI();
                break;
        }
    }

    private void UpdateHpUI()
    {
        if (textHp != null)
            textHp.text = $"{_playerAttribute.GetAttributeValue(AttributeType.HP)}";
    }

    private void UpdateAttackUI()
    {
        if (textAttack != null)
            textAttack.text = $"{_playerAttribute.GetAttributeValue(AttributeType.Attack)}";
    }

    private void UpdateDefenseUI()
    {
        if (textDefense != null)
            textDefense.text = $"{_playerAttribute.GetAttributeValue(AttributeType.Defense)}";
    }

    private void UpdateGoldUI()
    {
        if (textGold != null)
            textGold.text = $"{_playerAttribute.GetAttributeValue(AttributeType.Gold)}";
    }

    // 物品 UI 更新
    private void UpdateUI(ItemType type)
    {
        if (_inventoryService == null) return;
        switch (type)
        {
            case ItemType.All:
                UpdateYellowkeyUI();
                UpdateBluekeyUI();
                UpdateRedkeyUI();
                break;
            case ItemType.Key_Yellow:
                UpdateYellowkeyUI();
                break;
            case ItemType.Key_Blue:
                UpdateBluekeyUI();
                break;
            case ItemType.Key_Red:
                UpdateRedkeyUI();
                break;
        }
    }

    private void UpdateYellowkeyUI()
    {
        int count = _inventoryService.GetItemCount(ItemType.Key_Yellow);
        if (textYellowkey != null) textYellowkey.text = $"{count}";
    }

    private void UpdateBluekeyUI()
    {
        int count = _inventoryService.GetItemCount(ItemType.Key_Blue);
        if (textBluekey != null) textBluekey.text = $"{count}";
    }

    private void UpdateRedkeyUI()
    {
        int count = _inventoryService.GetItemCount(ItemType.Key_Red);
        if (textRedkey != null) textRedkey.text = $"{count}";
    }
}
