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

    [SerializeField] private Sprite defaultSprite; // 默认图标
    private ItemType _type = ItemType.None;
    private int _count = 0;
    private IInventoryService _inventory;

    [Inject]
    public void Construct(IInventoryService inventory)
    {
        _inventory = inventory;
    }

    private void Start()
    {
        Refresh();
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
}
