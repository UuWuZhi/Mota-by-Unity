using UnityEngine;
using TMPro; // 引入TextMeshPro命名空间
using System;
using VContainer;

public class UIItem : MonoBehaviour
{
    [Header("关联UI文本组件")]
    [SerializeField] private TextMeshProUGUI textYellowkey;  // 显示黄钥匙的Text
    [SerializeField] private TextMeshProUGUI textBluekey;    // 显示蓝钥匙的Text
    [SerializeField] private TextMeshProUGUI textRedkey;     // 显示红钥匙的Text

    private IInventoryService _inventoryService;

    // 使用构造注入（VContainer 会在场景组件注入时调用此方法）
    [Inject]
    public void Construct(IInventoryService inventory)
    {
        _inventoryService = inventory ?? throw new ArgumentNullException(nameof(inventory));
        _inventoryService.OnItemChanged += UpdateUI;
    }

    private void OnDestroy()
    {
        if (_inventoryService != null)
            _inventoryService.OnItemChanged -= UpdateUI;
    }

    private void Start()
    {
        // 游戏启动时初始化一次UI
        UpdateUI(ItemType.All);
    }


    // 【核心】更新所有属性的UI显示
    private void UpdateUI(ItemType type)
    {
        if (_inventoryService == null) return;

        switch (type)
        {
            case ItemType.All: // 全量更新（所有UI）
                UpdateYellowkeyUI();
                UpdateBluekeyUI();
                UpdateRedkeyUI();
                break;

            case ItemType.Key_Yellow: // 仅更新黄钥匙UI
                UpdateYellowkeyUI();
                break;

            case ItemType.Key_Blue: // 仅更新蓝钥匙UI
                UpdateBluekeyUI();
                break;

            case ItemType.Key_Red: // 仅更新红钥匙UI
                UpdateRedkeyUI();
                break;
        }
    }
    //单个UI的独立更新方法
    private void UpdateYellowkeyUI()
    {
        int count = _inventoryService.GetItemCount(ItemType.Key_Yellow);
        textYellowkey.text = $"{count}";
    }

    private void UpdateBluekeyUI()
    {
        int count = _inventoryService.GetItemCount(ItemType.Key_Blue);
        textBluekey.text = $"{count}";
    }

    private void UpdateRedkeyUI()
    {
        int count = _inventoryService.GetItemCount(ItemType.Key_Red);
        textRedkey.text = $"{count}";
    }
}