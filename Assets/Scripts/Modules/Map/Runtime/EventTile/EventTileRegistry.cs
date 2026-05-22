using System.Collections.Generic;
using Modules.Core.DataDefine;
using Modules.EventNodeSystem.DataDefine;
using Modules.EventNodeSystem.DataDefine.Data;
using Modules.Map.DataDefine;
using UnityEngine;

namespace Modules.Map.Runtime.EventTile
{
    /// <summary>
    ///     独立实现的事件瓦片注册表，负责按层管理 EventNodeTile 的内存索引。
    ///     不执行任何场景（GameObject）创建/销毁/移动操作，仅负责数据注册、查询与清理。
    /// </summary>
    public class EventTileRegistry : IEventTileRegistry
    {
        private readonly IGlobalEventVariables _globalEventVariables;
        private readonly Dictionary<int, Dictionary<Vector3Int, EventNodeTile>> _nodesByLayer = new();

        public EventTileRegistry(IGlobalEventVariables globalEventVariables = null)
        {
            _globalEventVariables = globalEventVariables;
        }

        public void RegisterEventTileAtCell(Vector3Int cellPos, EventNodeTile node, int? layerId = null)
        {
            if (node == null) return;
            var effectiveLayerId = ResolveLayerId(layerId);
            if (!_nodesByLayer.TryGetValue(effectiveLayerId, out var dict))
            {
                dict = new Dictionary<Vector3Int, EventNodeTile>();
                _nodesByLayer[effectiveLayerId] = dict;
            }

            node.cellPos = cellPos;
            dict[cellPos] = node;
        }

        public void RegisterEventTileAtWorldPos(Vector2 worldPos, EventNodeTile node, int? layerId = null)
        {
            if (node == null) return;
            // Registry does not depend on GridManager; use integer floor as stable mapping
            var cellPos = new Vector3Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y), 0);
            RegisterEventTileAtCell(cellPos, node, layerId);
        }

        public void UnregisterEventTileAtCell(Vector3Int cellPos, int? layerId = null)
        {
            var effectiveLayerId = ResolveLayerId(layerId);
            if (_nodesByLayer.TryGetValue(effectiveLayerId, out var dict)) dict.Remove(cellPos);
        }

        public void UnregisterEventTileAtWorldPos(Vector2 worldPos, int? layerId = null)
        {
            var cellPos = new Vector3Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y), 0);
            UnregisterEventTileAtCell(cellPos, layerId);
        }

        public bool TryGetEventNodeAtCell(Vector3Int cellPos, out EventNodeTile node, int? layerId = null)
        {
            node = null;
            var effectiveLayerId = ResolveLayerId(layerId);
            if (!_nodesByLayer.TryGetValue(effectiveLayerId, out var dict)) return false;
            return dict.TryGetValue(cellPos, out node) && node != null;
        }

        public void ClearLayer(int? layerId = null)
        {
            var effectiveLayerId = ResolveLayerId(layerId);
            if (_nodesByLayer.TryGetValue(effectiveLayerId, out var dict))
                dict.Clear();
        }

        public IReadOnlyDictionary<Vector3Int, EventNodeTile> GetLayerRegistry(int? layerId = null)
        {
            var effectiveLayerId = ResolveLayerId(layerId);
            if (_nodesByLayer.TryGetValue(effectiveLayerId, out var dict))
                return new Dictionary<Vector3Int, EventNodeTile>(dict);
            return new Dictionary<Vector3Int, EventNodeTile>();
        }

        private int ResolveLayerId(int? layerId)
        {
            if (layerId.HasValue) return layerId.Value;
            return _globalEventVariables != null ? _globalEventVariables.GetInt(GlobalEventKey.LayerId) : 0;
        }
    }
}