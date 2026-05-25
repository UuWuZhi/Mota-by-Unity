using System.Collections.Generic;
using Modules.EventNodeSystem.DataDefine;
using UnityEngine;

namespace Modules.Map.DataDefine
{
    /// <summary>
    ///     事件瓦片注册表接口：提供注册/注销/查询按层管理的 EventNodeTile 的最小操作集合。
    ///     设计目标是与具体场景管理（GameObject 操作）解耦，便于测试与替换实现。
    /// </summary>
    public interface IEventTileRegistry
    {
        void RegisterEventTileAtCell(Vector3Int cellPos, EventNodeTile node, int? layerId = null);
        void RegisterEventTileAtWorldPos(Vector2 worldPos, EventNodeTile node, int? layerId = null);

        void UnregisterEventTileAtCell(Vector3Int cellPos, int? layerId = null);
        void UnregisterEventTileAtWorldPos(Vector2 worldPos, int? layerId = null);

        bool TryGetEventNodeAtCell(Vector3Int cellPos, out EventNodeTile node, int? layerId = null);

        /// <summary>
        ///     清理指定层的注册表条目（仅修改注册表，不销毁 GameObject）。
        /// </summary>
        void ClearLayer(int? layerId = null);

        /// <summary>
        ///     返回指定层的只读注册表视图（若无返回空字典）。
        /// </summary>
        IReadOnlyDictionary<Vector3Int, EventNodeTile> GetLayerRegistry(int? layerId = null);
    }
}