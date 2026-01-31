using UnityEngine;
using VContainer;

/// <summary>
/// Pickaxe 使用逻辑：判断玩家面向一格处是否有 ObstacleTile，若存在且可被 Pickaxe 破坏则移除并消费道具。
/// 通过 DI 获取需要的服务：PlayerState（位置/朝向）、GridManager（Tilemap 操作）、PlayerInventory（道具消耗）、EventCenter（事件通知）。
/// </summary>
public class Pickaxe : MonoBehaviour
{
    [Inject] private PlayerState _playerState;
    [Inject] private GridManager _gridManager;
    [Inject] private PlayerInventory _inventory;

    // 调用此方法执行破坏尝试
    public void TryUse()
    {
        if (_playerState == null || _gridManager == null || _inventory == null) return;

        // 计算面朝方向前方一格的单元格坐标
        Vector3Int playerCell = _playerState.CellPos;
        Vector3Int targetCell = playerCell;
        switch (_playerState.Facing)
        {
            case Facing.Up: targetCell += new Vector3Int(0, 1, 0); break;
            case Facing.Down: targetCell += new Vector3Int(0, -1, 0); break;
            case Facing.Left: targetCell += new Vector3Int(-1, 0, 0); break;
            case Facing.Right: targetCell += new Vector3Int(1, 0, 0); break;
        }

        // 检查目标格子是否在边界内
        if (!_gridManager.IsInGridBounds(targetCell)) return;

        // 获取 ObstacleTile（若无则不处理）
        var obstacle = _gridManager.GetObstacleTileAtCell(targetCell);
        if (obstacle == null) return;

        // 判断是否可破坏
        if (!obstacle.IsBreakable) return;

        // 判断 Pickaxe 是否为可用的道具（部分障碍需要特定道具）
        if (obstacle.BreakableBy != null && obstacle.BreakableBy.Count > 0)
        {
            bool hasRequired = false;
            foreach (var it in obstacle.BreakableBy)
            {
                if (it == ItemType.Pickaxe && _inventory.HasItem(it)) { hasRequired = true; break; }
            }
            if (!hasRequired) return; // 没有合适的道具，不能破坏
        }

        // 若满足条件：移除障碍瓦片
        bool removed = _gridManager.RemoveObstacleTileAtCell(targetCell);
        if (!removed) return;
        _inventory.RemoveItem(ItemType.Pickaxe, 1);
    }
}
