using System;
using UnityEngine;

[CreateAssetMenu(fileName = "RemoveTileAction", menuName = "EventNodes/Action/RemoveTile")]
public class RemoveTileAction : ActionNode
{
    public override void Execute(EventNodeContext ctx, Action onComplete)
    {
        //Debug.Log("开始移除瓦片");
        if (ctx.GridManager != null)
        {
            Vector3Int cell = ctx.CellPos;
            // 使用 GridManager 提供的移除接口（移除 Tilemap 上的瓦片）
            ctx.GridManager.RemoveEventTile(cell);

            // 如果存在运行时创建或手动创建的承载 GameObject，注销并销毁它
            if (ctx.EventNodeManager != null && ctx.EventNodeManager.TryGetEventNodeAtCell(cell, out var node))
            {
                try
                {
                    ctx.EventNodeManager.UnregisterEventNodeAtCell(cell);
                    if (node != null)
                    {
                        GameObject.Destroy(node.gameObject);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
        else
        {
            Debug.LogWarning("RemoveTileAction: GridManager 未初始化");
        }

        onComplete?.Invoke();
    }
}