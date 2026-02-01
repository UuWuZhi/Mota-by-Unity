using System;
using UnityEngine;

[CreateAssetMenu(fileName = "RemoveTileAction", menuName = "EventNodes/Action/RemoveTile")]
public class RemoveTileAction : TileActionNode
{
    public override void ExecuteTile(EventNodeTileContext ctx, Action onComplete)
    {
        if (ctx.GridManager == null)
        {
            Debug.LogWarning("RemoveTileAction: GridManager 未初始化");
            onComplete?.Invoke();
            return;
        }
        Vector3Int cell = ctx.CellPos;

        ctx.GridManager.RemoveEventTileAtCell(cell);

        onComplete?.Invoke();
    }
}