using UnityEngine;

/// <summary>
/// 简易测试脚本：按 E 切换背包面板的显示（用于测试期）
/// 不考虑其他 UI 状态，直接切换 InventoryUI.root 的 active 状态。
/// 将此脚本挂到场景中任意 GameObject，或者在 Inspector 手动指定 targetInventoryUI。
/// </summary>
public class TestInventoryToggle : MonoBehaviour
{
    [Tooltip("可选：在 Inspector 指定要控制的 InventoryUI（若为空会在运行时查找）")]
    public InventoryUI targetInventoryUI;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            ToggleInventory();
        }
    }

    private void ToggleInventory()
    {
        var inv = targetInventoryUI;
        if (inv == null)
        {
            inv = GameObject.FindObjectOfType<InventoryUI>();
            if (inv == null)
            {
                Debug.LogWarning("TestInventoryToggle: 未找到 InventoryUI 实例，无法切换");
                return;
            }
        }

        if (inv.root == null)
        {
            Debug.LogWarning("TestInventoryToggle: InventoryUI.root 为 null，无法切换");
            return;
        }

        bool newState = !inv.root.activeSelf;
        inv.root.SetActive(newState);
        Debug.Log($"Inventory root active set to {newState}");
    }
}
