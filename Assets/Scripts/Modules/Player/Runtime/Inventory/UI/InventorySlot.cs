using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// 单个背包格的 UI 组件：显示图标与数量，并响应点击以选中/使用等。
/// 需要在 Inspector 关联 iconImage 与 countText
/// </summary>
public class InventorySlot : MonoBehaviour
{
    public Image iconImage;             // 图标图片组件
    public TextMeshProUGUI countText;   // 数量文本组件
    public Button slotButton;           // 可选：槽位上的 Button 组件，用于接收点击事件

    [SerializeField] private Sprite defaultSprite; // 默认图标
    private ItemType _type = ItemType.None;
    private int _count = 0;
    private IInventoryService _inventory;

    private ItemUseHandler _useHandler;
    private ItemDatabase _itemDatabase;

    [Inject]
    public void Construct(IInventoryService inventory, ItemUseHandler useHandler, ItemDatabase itemDatabase)
    {
        _inventory = inventory;
        _useHandler = useHandler;
        _itemDatabase = itemDatabase;
    }

    private void Start()
    {
        Refresh();
    }

    private void OnEnable()
    {
        if (slotButton == null) slotButton = GetComponent<Button>();
        if (slotButton != null) slotButton.onClick.AddListener(OnSlotClicked);
    }

    private void OnDisable()
    {
        if (slotButton != null) slotButton.onClick.RemoveListener(OnSlotClicked);
    }

    /// <summary>
    /// 绑定数据到槽位（UI 层调用）
    /// </summary>
    public void SetData(ItemType type, int count, Sprite icon = null)
    {
        _type = type;
        _count = count;
        if (iconImage != null && icon != null) iconImage.sprite = icon;
        Refresh();
    }

    public void Clear()
    {
        _type = ItemType.None;
        _count = 0;
        if (iconImage != null) iconImage.sprite = defaultSprite;
        if (countText != null) countText.text = "";
    }

    public void Refresh()
    {
        if (_type == ItemType.None)
        {
            if (countText != null) countText.text = "";
            return;
        }

        // 优先使用本地缓存的 count，若无则从服务查询（兼容）
        int displayCount = _count;
        if (_inventory != null)
        {
            int serviceTotal = _inventory.GetItemCount(_type);
            // 若服务返回更大的值，显示服务的聚合数（保持一致性）
            if (serviceTotal != 0) displayCount = serviceTotal;
        }

        if (countText != null) countText.text = displayCount > 0 ? displayCount.ToString() : "";
    }

    // 点击槽位时尝试使用该格物品
    private void OnSlotClicked()
    {
        Debug.Log($"InventorySlot: 使用物品 {_type}，数量 {_count}");
        if (_type == ItemType.None)
        {
            Debug.Log("InventorySlot: 空槽位被点击，无物品可用");
            return;
        }
        if (_inventory == null)
        {
            Debug.LogWarning("InventorySlot: 无法访问 InventoryService，无法使用物品");
            return;
        }
        if (!_inventory.HasItem(_type))
        {
            Debug.Log("InventorySlot: 没有该物品，无法使用");
            return;
        }

        if (_itemDatabase == null || _useHandler == null)
        {
            Debug.LogWarning("InventorySlot: missing ItemDatabase or ItemUseHandler, cannot use item");
            return;
        }

        var data = _itemDatabase.Get(_type);
        if (data == null)
        {
            Debug.LogWarning($"InventorySlot: no ItemData for type {_type}");
            return;
        }
        Debug.Log($"InventorySlot: found ItemData for {_type}, useMode={data.useMode}, useNodes count={data.useNodes?.Count ?? 0}");
        // 使用统一的 ItemUseHandler 来执行物品逻辑（会构建 Context 并处理消耗）
        var player = FindPlayerGameObject();
        _useHandler?.UseItem(data, caller: player, worldPos: null, slotIndex: -1, count: 1, onComplete: () =>
        {
            Refresh();
        });
    }

    private GameObject FindPlayerGameObject()
    {
        var ps = FindObjectOfType<PlayerState>();
        return ps != null ? ps.gameObject : null;
    }
}
