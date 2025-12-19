using UnityEngine;
using TMPro; // 引入TextMeshPro命名空间

public class ItemUIManager : MonoBehaviour
{
    [Header("关联UI文本组件")]
    [SerializeField] private TextMeshProUGUI textYellowkey;  // 显示黄钥匙的Text
    [SerializeField] private TextMeshProUGUI textBluekey;    // 显示蓝钥匙的Text
    [SerializeField] private TextMeshProUGUI textRedkey;     // 显示红钥匙的Text

    private PlayerInventory _playerInventory;


    private void Awake()
    {
        // 获取实例
        _playerInventory = PlayerInventory.Instance;
        if (_playerInventory == null)
        {
            Debug.LogError("场景中找不到PlayerInventory实例！");
            return;
        }
    }

    private void OnEnable()
    {
        // 订阅属性变化事件：属性变了就更新UI
        _playerInventory.OnItemChanged += UpdateUI;
    }

    private void OnDisable()
    {
        // 取消订阅（避免内存泄漏）
        _playerInventory.OnItemChanged -= UpdateUI;
    }

    private void Start()
    {
        // 游戏启动时初始化一次UI
        UpdateUI(ItemType.All);
    }


    // 【核心】更新所有属性的UI显示
    private void UpdateUI(ItemType type)
    {
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
        textYellowkey.text = $"{_playerInventory.GetItemCount(ItemType.Key_Yellow)}";
    }

    private void UpdateBluekeyUI()
    {
        textBluekey.text = $"{_playerInventory.GetItemCount(ItemType.Key_Blue)}";
    }

    private void UpdateRedkeyUI()
    {
        textRedkey.text = $"{_playerInventory.GetItemCount(ItemType.Key_Red)}";
    }
}