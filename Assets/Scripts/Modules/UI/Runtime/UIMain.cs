using System;
using Modules.Core.DataDefine;
using Modules.EventSystem.DataDefine.EventArgs;
using Modules.Item.DataDefine;
using Modules.Player.DataDefine;
using Modules.Player.Runtime.Attribute;
using TMPro;
using UnityEngine;
using VContainer;

// 将属性与物品 UI 合并为单个主 UI 类，并继承自 BaseUI
namespace Modules.UI.Runtime
{
    public class UIMain : BaseUI
    {
        [Header("属性 UI")] [SerializeField] private TextMeshProUGUI textHp;

        [SerializeField] private TextMeshProUGUI textAttack;
        [SerializeField] private TextMeshProUGUI textDefense;
        [SerializeField] private TextMeshProUGUI textGold;

        [SerializeField] private TextMeshProUGUI textYellowKey;
        [SerializeField] private TextMeshProUGUI textBlueKey;
        [SerializeField] private TextMeshProUGUI textRedKey;
        private IInventoryService _inventoryService;

        private PlayerAttribute _playerAttribute;
        private bool _subscribed;

        protected override bool ShowOnAwake => true;

        private void Start()
        {
            // 初始化 UI 显示
            if (_playerAttribute) UpdateUI(AttributeType.All);
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

        // VContainer 注入
        [Inject]
        public void Construct(PlayerAttribute playerAttribute, IInventoryService inventory)
        {
            _playerAttribute = playerAttribute;
            _inventoryService = inventory;
        }

        private void SubscribeEventCenter()
        {
            if (_subscribed) return;
            if (_playerAttribute) _playerAttribute.AttributeChanged += OnAttributeChanged;
            if (_inventoryService != null) _inventoryService.InventoryChanged += OnInventoryChanged_EventCenter;
            _subscribed = true;
        }

        private void UnsubscribeEventCenter()
        {
            if (!_subscribed) return;
            if (_playerAttribute) _playerAttribute.AttributeChanged -= OnAttributeChanged;
            if (_inventoryService != null) _inventoryService.InventoryChanged -= OnInventoryChanged_EventCenter;
            _subscribed = false;
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
        private void UpdateUI(AttributeType itemType)
        {
            if (!_playerAttribute) return;
            switch (itemType)
            {
                case AttributeType.All:
                    UpdateHpUI();
                    UpdateAttackUI();
                    UpdateDefenseUI();
                    UpdateGoldUI();
                    break;
                case AttributeType.Hp:
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
            if (textHp)
                textHp.text = $"{_playerAttribute.GetAttributeValue(AttributeType.Hp)}";
        }

        private void UpdateAttackUI()
        {
            if (textAttack)
                textAttack.text = $"{_playerAttribute.GetAttributeValue(AttributeType.Attack)}";
        }

        private void UpdateDefenseUI()
        {
            if (textDefense)
                textDefense.text = $"{_playerAttribute.GetAttributeValue(AttributeType.Defense)}";
        }

        private void UpdateGoldUI()
        {
            if (textGold)
                textGold.text = $"{_playerAttribute.GetAttributeValue(AttributeType.Gold)}";
        }

        // 物品 UI 更新
        private void UpdateUI(ItemType itemType)
        {
            if (_inventoryService == null) return;
            switch (itemType)
            {
                case ItemType.All:
                    UpdateYellowkeyUI();
                    UpdateBluekeyUI();
                    UpdateRedkeyUI();
                    break;
                case ItemType.KeyYellow:
                    UpdateYellowkeyUI();
                    break;
                case ItemType.KeyBlue:
                    UpdateBluekeyUI();
                    break;
                case ItemType.KeyRed:
                    UpdateRedkeyUI();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null);
            }
        }

        private void UpdateYellowkeyUI()
        {
            var count = _inventoryService.GetItemCount(ItemType.KeyYellow);
            if (textYellowKey) textYellowKey.text = $"{count}";
        }

        private void UpdateBluekeyUI()
        {
            var count = _inventoryService.GetItemCount(ItemType.KeyBlue);
            if (textBlueKey) textBlueKey.text = $"{count}";
        }

        private void UpdateRedkeyUI()
        {
            var count = _inventoryService.GetItemCount(ItemType.KeyRed);
            if (textRedKey) textRedKey.text = $"{count}";
        }
    }
}