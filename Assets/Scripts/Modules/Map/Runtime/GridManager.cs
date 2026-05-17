// GridManager：基础网格管理系统

using System;
using Modules.Core.DataDefine;
using Modules.EventSystem.DataDefine.EventArgs;
using Modules.Map.DataDefine;
using Modules.Map.DataDefine.Tile;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;
using VContainer;

namespace Modules.Map.Runtime
{
    public class GridManager : MonoBehaviour
    {
        public float tileSize = 1f; // 单个格子尺寸（和Tilemap的cell size一致）
        [FormerlySerializedAs("MapGrid")] public Grid mapGrid; // 由外部注入，不再从MapManager获取
        private Tilemap _currentGroundTilemap; // 当前层地面层引用
        private BoundsInt _currentLayerBounds; // 缓存当前层边界
        private Tilemap _currentObstacleTilemap; // 当前层障碍层引用
        private EventCenter _eventCenter;

        private bool _eventSubscribed;

        private IGlobalEventVariables _globalEventVariables;
        public Tilemap CurrentEventTilemap { get; private set; }

        // 事件瓦片变化（局部事件，供 EventTileManager 订阅）
        public event EventHandler<TileMovedEventArgs> OnEventTileMoved;
        public event EventHandler<TileRemovedEventArgs> OnEventTileRemoved;

        #region 生命周期

        [Inject]
        public void Construct(IGlobalEventVariables globalEventVariables, EventCenter eventCenter)
        {
            _globalEventVariables = globalEventVariables;
            _eventCenter = eventCenter;
            SubscribeEventCenter();
        }

        private void OnEnable()
        {
            SubscribeEventCenter();
        }

        private void OnDisable()
        {
            UnsubscribeEventCenter();
        }

        #endregion

        #region 事件系统

        private void OnLayerSwitchedHandler(object sender, EventArgs args)
        {
            if (args is not LayerSwitchedEventArgs layerArgs || !layerArgs.GroundTilemap ||
                !layerArgs.ObstacleTilemap || !layerArgs.EventTilemap)
            {
                Debug.LogError("GridManager：楼层切换事件数据无效！");
                return;
            }

            // 更新当前层数据
            UpdateCurrentLayerData(layerArgs.GroundTilemap, layerArgs.ObstacleTilemap, layerArgs.EventTilemap,
                layerArgs.LayerBounds);
        }

        private void SubscribeEventCenter()
        {
            if (!_eventCenter || _eventSubscribed) return;
            _eventCenter.OnLayerSwitched += OnLayerSwitchedHandler;
            _eventSubscribed = true;
        }

        private void UnsubscribeEventCenter()
        {
            if (!_eventCenter || !_eventSubscribed) return;
            _eventCenter.OnLayerSwitched -= OnLayerSwitchedHandler;
            _eventSubscribed = false;
        }

        #endregion

        #region 地图加载

        // 更新数据至当前层
        public void UpdateCurrentLayerData(Tilemap groundTilemap, Tilemap obstacleTilemap, Tilemap eventTilemap,
            BoundsInt layerBounds)
        {
            _currentGroundTilemap = groundTilemap;
            _currentObstacleTilemap = obstacleTilemap;
            CurrentEventTilemap = eventTilemap;
            _currentLayerBounds = layerBounds; // 直接使用传入的边界
            LoadMap(); // 重新加载当前层的网格数据
        }

        //加载地图数据
        public void LoadMap()
        {
            if (!mapGrid)
            {
                Debug.LogError("LoadMap失败：MapGrid未初始化（请先调用SetMapGrid）");
                return;
            }

            if (!_currentGroundTilemap || !_currentObstacleTilemap || !CurrentEventTilemap)
            {
                Debug.LogError("LoadMap失败：当前层Tilemap为null");
                return;
            }

            if (!IsBoundsValid(_currentLayerBounds)) Debug.LogError("LoadMap失败：层边界无效:{_currentLayerBounds}");
            // 不再在内存中缓存基础层格子类型。读取时按需直接查询对应 Tilemap。
            // Debug.Log("LoadMap: 当前层 Tilemap 已设置（Ground/Obstacle/Event），按需查询 Tilemap");
        }

        #endregion

        #region 瓦片操作

        #region 查询

        /// <summary>
        ///     直接查询指定世界坐标在当前层指定图层上的 Tile
        /// </summary>
        public TileBase GetTileAtWorldPos(Vector2 worldPos, TileMapType tileMapType)
        {
            if (!mapGrid) return null;
            var cellPos = mapGrid.WorldToCell(worldPos);
            return GetTileAtCell(cellPos, tileMapType);
        }

        public TileBase GetTileAtCell(Vector3Int cellPos, TileMapType tileMapType)
        {
            if (!mapGrid) return null;
            if (!IsInGridBounds(cellPos)) return null;
            return tileMapType switch
            {
                TileMapType.Ground => _currentGroundTilemap.GetTile(cellPos),
                TileMapType.Obstacle => _currentObstacleTilemap.GetTile(cellPos),
                TileMapType.Event => CurrentEventTilemap.GetTile(cellPos),
                _ => null
            };
        }

        /// <summary>
        ///     泛型重载：根据 TileMapType 返回指定派生类型的 Tile（若类型不匹配返回 null）
        /// </summary>
        public T GetTileAtWorldPos<T>(Vector2 worldPos, TileMapType tileMapType) where T : TileBase
        {
            var tile = GetTileAtWorldPos(worldPos, tileMapType);
            return tile as T;
        }

        public T GetTileAtCell<T>(Vector3Int cellPos, TileMapType tileMapType) where T : TileBase
        {
            var tile = GetTileAtCell(cellPos, tileMapType);
            return tile as T;
        }

        public DataDefine.Tile.EventTile GetEventTileAtCell(Vector3Int cellPos)
        {
            return GetTileAtCell<DataDefine.Tile.EventTile>(cellPos, TileMapType.Event);
        }

        public DataDefine.Tile.EventTile GetEventTileAtWorldPos(Vector2 worldPos)
        {
            return GetTileAtWorldPos<DataDefine.Tile.EventTile>(worldPos, TileMapType.Event);
        }

        public ObstacleTile GetObstacleTileAtCell(Vector3Int cellPos)
        {
            return GetTileAtCell<ObstacleTile>(cellPos, TileMapType.Obstacle);
        }

        public ObstacleTile GetObstacleTileAtWorldPos(Vector2 worldPos)
        {
            return GetTileAtWorldPos<ObstacleTile>(worldPos, TileMapType.Obstacle);
        }

        public GroundTile GetGroundTileAtCell(Vector3Int cellPos)
        {
            return GetTileAtCell<GroundTile>(cellPos, TileMapType.Ground);
        }

        public GroundTile GetGroundTileAtWorldPos(Vector2 worldPos)
        {
            return GetTileAtWorldPos<GroundTile>(worldPos, TileMapType.Ground);
        }

        #endregion

        #region 移除

        public bool RemoveTileAtCell(Vector3Int cellPos, TileMapType tileMapType)
        {
            if (!mapGrid) return false;
            if (!IsInGridBounds(cellPos)) return false;
            var targetTilemap = tileMapType switch
            {
                TileMapType.Ground => _currentGroundTilemap,
                TileMapType.Obstacle => _currentObstacleTilemap,
                TileMapType.Event => CurrentEventTilemap,
                _ => null
            };
            if (!targetTilemap) return false;
            var removed = CurrentEventTilemap.GetTile(cellPos) as BaseTile;
            CurrentEventTilemap.SetTile(cellPos, null);

            var layerId = _globalEventVariables.GetInt(GlobalEventKey.LayerId);
            targetTilemap.SetTile(cellPos, null);
            OnEventTileRemoved?.Invoke(this, new TileRemovedEventArgs
            {
                Cell = cellPos,
                LayerId = layerId,
                TileAsset = removed
            });
            return true;
        }

        public void TriggerEventTileMoved(TileMovedEventArgs args)
        {
            OnEventTileMoved?.Invoke(this, args);
        }

        public bool RemoveTileAtWorldPos(Vector2 worldPos, TileMapType tileMapType)
        {
            if (!mapGrid) return false;
            var cellPos = mapGrid.WorldToCell(worldPos);
            return RemoveTileAtCell(cellPos, tileMapType);
        }

        public bool RemoveEventTileAtCell(Vector3Int cellPos)
        {
            return RemoveTileAtCell(cellPos, TileMapType.Event);
        }

        public bool RemoveEventTileAtWorldPos(Vector2 worldPos)
        {
            return RemoveTileAtWorldPos(worldPos, TileMapType.Event);
        }

        public bool RemoveObstacleTileAtCell(Vector3Int cellPos)
        {
            return RemoveTileAtCell(cellPos, TileMapType.Obstacle);
        }

        public bool RemoveObstacleTileAtWorldPos(Vector2 worldPos)
        {
            return RemoveTileAtWorldPos(worldPos, TileMapType.Obstacle);
        }

        public bool RemoveGroundTileAtCell(Vector3Int cellPos)
        {
            return RemoveTileAtCell(cellPos, TileMapType.Ground);
        }

        public bool RemoveGroundTileAtWorldPos(Vector2 worldPos)
        {
            return RemoveTileAtWorldPos(worldPos, TileMapType.Ground);
        }

        #endregion

        #endregion

        #region 坐标与边界

        public bool TryConvertWorldToGridPos(Vector2 worldPos, out int gridX, out int gridY) // 世界坐标转网格坐标（带验证）
        {
            gridX = -1;
            gridY = -1;
            if (!mapGrid)
                return false;
            var cellPos = mapGrid.WorldToCell(worldPos);
            return TryConvertCellToGridPos(cellPos, out gridX, out gridY);
        }

        public bool TryConvertCellToGridPos(Vector3Int cellPos, out int gridX, out int gridY) // 单元格坐标转网格坐标（带验证）
        {
            gridX = cellPos.x - _currentLayerBounds.xMin;
            gridY = cellPos.y - _currentLayerBounds.yMin;
            return IsInGridBounds(gridX, gridY);
        }

        public bool TryConvertGridToWorldPos(int gridX, int gridY, out Vector2 worldPos) // 网格坐标转世界坐标（带验证）
        {
            worldPos = Vector2.zero;
            if (!IsInGridBounds(gridX, gridY))
            {
                Debug.LogWarning($"网格坐标 ({gridX},{gridY}) 超出边界");
                return false;
            }

            // 网格坐标 → 单元格坐标（基于当前层边界）
            var cellX = gridX + _currentLayerBounds.xMin;
            var cellY = gridY + _currentLayerBounds.yMin;
            var cellPos = new Vector3Int(cellX, cellY, 0);
            // 单元格坐标 → 世界坐标（中心位置）
            worldPos = mapGrid.GetCellCenterWorld(cellPos);
            return true;
        }

        public bool TryConvertWorldToCellPos(Vector2 worldPos, out Vector3Int cellPos) // 世界坐标转单元格坐标（带验证）
        {
            cellPos = Vector3Int.zero;
            if (!mapGrid)
                return false;
            cellPos = mapGrid.WorldToCell(worldPos);
            return IsInGridBounds(cellPos);
        }

        public bool IsInGridBounds(Vector3Int cellPos) // 边界检查重载1：单元格坐标判断
        {
            if (IsBoundsValid(_currentLayerBounds))
                return cellPos.x >= _currentLayerBounds.xMin && cellPos.x < _currentLayerBounds.xMax
                                                             && cellPos.y >= _currentLayerBounds.yMin &&
                                                             cellPos.y < _currentLayerBounds.yMax;
            Debug.LogWarning("IsInGridBounds:当前层边界非法");
            return false;
        }

        public bool IsInGridBounds(int gridX, int gridY) // 边界检查重载2：网格坐标判断
        {
            if (!IsBoundsValid(_currentLayerBounds))
            {
                Debug.LogWarning("IsInGridBounds:当前层边界非法");
                return false;
            }

            var width = _currentLayerBounds.size.x;
            var height = _currentLayerBounds.size.y;
            return gridX >= 0 && gridX < width && gridY >= 0 && gridY < height;
        }

        public bool IsBoundsValid(BoundsInt bounds) //边界检查基础：边界合法
        {
            return bounds.size is { x: > 0, y: > 0 };
        }

        public Vector2 GetCellCenterWorld(Vector2 worldPos) //获取格子中心世界坐标
        {
            if (!mapGrid)
            {
                Debug.LogError("GetCellCenterWorld失败：MapGrid为null");
                return worldPos;
            }

            var cellPos = mapGrid.WorldToCell(worldPos);
            return mapGrid.GetCellCenterWorld(cellPos);
        }

        // 新增重载：直接使用单元格坐标获取格子中心世界坐标，统一入口
        public Vector2 GetCellCenterWorld(Vector3Int cellPos)
        {
            if (mapGrid) return mapGrid.GetCellCenterWorld(cellPos);
            Debug.LogError("GetCellCenterWorld失败：MapGrid为null");
            return Vector2.zero;
        }

        private BoundsInt GetMapTotalBounds(Tilemap tilemap) //用于计算单张地图的边界（不知道有啥用，先留着吧）
        {
            var tilemapBounds = tilemap.cellBounds;
            if (tilemapBounds.size.x <= 0 || tilemapBounds.size.y <= 0) return new BoundsInt(0, 0, 0, 1, 1, 1); // 默认空边界
            return tilemapBounds;
        }

        #endregion
    }
}