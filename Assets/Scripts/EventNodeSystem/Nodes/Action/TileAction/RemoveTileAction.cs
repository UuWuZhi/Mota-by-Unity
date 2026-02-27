using System;
using UnityEngine;

[CreateAssetMenu(fileName = "RemoveTileAction", menuName = "EventNodes/Action/RemoveTile")]
public class RemoveTileAction : TileActionNode
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
            Debug.LogWarning("RemoveTileAction: GridManager 未初始化");
            onComplete?.Invoke();
            return;
        }
        Vector3Int cell = ctx.CellPos;

        gridManager.RemoveEventTileAtCell(cell);

        onComplete?.Invoke();
    }
}