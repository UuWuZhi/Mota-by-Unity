using UnityEngine;
using TMPro; // 引入TextMeshPro命名空间

public class ItemUIManager : MonoBehaviour
{
    [Header("关联UI文本组件")]
    [SerializeField] private TextMeshProUGUI textYellowkey;  // 显示黄钥匙的Text
    [SerializeField] private TextMeshProUGUI textBluekey;    // 显示蓝钥匙的Text
    [SerializeField] private TextMeshProUGUI textRedkey;     // 显示红钥匙的Text

    private IInventoryService _inventoryService;

    private void Awake()
    {
        // 仅使用注入/适配器提供的服务，删除对 PlayerInventory.Instance 的直接依赖
        _inventoryService = InventoryAdapter.Current;
        if (_inventoryService == null)
        {
            Debug.LogError("ItemUIManager: 未配置 InventoryService（InventoryAdapter.Current 为 null）。请在 DiBootstrap 中注册 InventoryAdapter。");
            enabled = false; // 禁用组件以避免后续空引用
            return;
        }
    }

    private void OnEnable()
    {
        _inventoryService.OnItemChanged += UpdateUI;
    }

    private void OnDisable()
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