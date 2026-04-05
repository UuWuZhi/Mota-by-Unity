using System;
using UnityEngine;

[CreateAssetMenu(fileName = "RemoveTile", menuName = "EventNodes/Action/RemoveTile")]
public class RemoveTile : TileActionNode
{
    public override Type[] GetRequiredServices()
    {
        return new[] { typeof(GridManager) };
    }

    public override void ExecuteTile(EventNodeTileContext ctx, Action onComplete)
    {
        var gridManager = ctx?.GetService<GridManager>();
        if (gridManager == null)
        {
            Debug.LogWarning("RemoveTile: GridManager 未初始化");
            onComplete?.Invoke();
            return;
        }
        Vector3Int cell = ctx.CellPos;

        gridManager.RemoveEventTileAtCell(cell);

        onComplete?.Invoke();
    }
}