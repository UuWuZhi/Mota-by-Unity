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
    /// <summary>
    /// 管理地图网格和当前层的 Tilemap，提供瓦片查询、移除、地图加载与世界/格子坐标转换，并通过事件公开局部瓦片变更。
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        public float tileSize = 1f; // 和Tilemap的cell size一致
        [FormerlySerializedAs("MapGrid")] [SerializeField] private Grid mapGrid;
        private Tilemap _currentGroundTilemap;
        private Tilemap _currentObstacleTilemap; 
        public Tilemap CurrentEventTilemap { get; private set; }
        private BoundsInt _currentLayerBounds;

        private bool _eventSubscribed;

        private EventCenter _eventCenter;
        private IGlobalEventVariables _globalEventVariables;

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

        /// <summary>
        /// 更新当前层的瓦片图引用并重新加载该层的网格数据。
        /// </summary>
        /// <remarks>方法会设置内部字段并调用 LoadMap 来重新加载当前层的网格数据；不对参数进行空值检查，调用方应确保传入参数有效。</remarks>
        /// <param name="groundTilemap">当前层的地面 Tilemap 引用。</param>
        /// <param name="obstacleTilemap">当前层的障碍物 Tilemap 引用。</param>
        /// <param name="eventTilemap">当前层的事件 Tilemap 引用。</param>
        /// <param name="layerBounds">当前层在网格坐标中的边界范围，用于确定需要加载的区域。</param>
        public void UpdateCurrentLayerData(Tilemap groundTilemap, Tilemap obstacleTilemap, Tilemap eventTilemap,
            BoundsInt layerBounds)
        {
            _currentGroundTilemap = groundTilemap;
            _currentObstacleTilemap = obstacleTilemap;
            CurrentEventTilemap = eventTilemap;
            _currentLayerBounds = layerBounds;
            LoadMap(); // 重新加载当前层的网格数据
        }

        /// <summary>
        /// 加载并初始化当前层地图，验证 MapGrid、Ground/Obstacle/Event Tilemap 是否已设置以及层边界的有效性。
        /// </summary>
        /// <remarks>若 MapGrid 或任一当前层 Tilemap 未初始化，则记录错误并返回。基础层格子类型不再在内存中缓存，按需直接从对应 Tilemap 查询。</remarks>
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
        }

        #endregion

        #region 瓦片操作

        #region 查询

        /// <summary>
        /// 返回指定格子位置和瓦片图层类型对应的 TileBase。
        /// </summary>
        /// <remarks>当 mapGrid 未设置或 cellPos 不在网格范围内时返回 null；根据 tileMapType 从相应 Tilemap 获取瓦片。</remarks>
        /// <param name="cellPos">要查询的格子位置（Vector3Int，格子坐标）。</param>
        /// <param name="tileMapType">要查询的瓦片图层类型（例如地面、障碍、事件）。</param>
        /// <returns>对应的 TileBase 实例；若地图未初始化、位置越界或未找到瓦片则返回 null。</returns>
        private TileBase GetTileAtCell(Vector3Int cellPos, TileMapType tileMapType)
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
        /// 获取指定单元格处的瓦片并将其转换为类型 T。
        /// </summary>
        /// <remarks>使用安全转换（as），在类型不匹配时不会抛出异常。</remarks>
        /// <typeparam name="T">要返回的瓦片类型，必须继承自 TileBase。</typeparam>
        /// <param name="cellPos">目标单元格的格子坐标。</param>
        /// <param name="tileMapType">要查询的瓦片图类型。</param>
        /// <returns>返回转换为 T 的瓦片实例；如果瓦片不存在或无法转换，则返回 null。</returns>
        private T GetTileAtCell<T>(Vector3Int cellPos, TileMapType tileMapType) where T : TileBase
        {
            var tile = GetTileAtCell(cellPos, tileMapType);
            return tile as T;
        }

        /// <summary>
        /// 获取指定单元格位置上的事件图块（来自 TileMapType.Event 图层）。
        /// </summary>
        /// <remarks>调用通用方法 GetTileAtCell T 并将图层类型指定为 TileMapType.Event。</remarks>
        /// <param name="cellPos">要查询的瓦片单元格位置，使用 Vector3Int 表示的瓦片坐标。</param>
        /// <returns>找到则返回对应的 DataDefine.Tile.EventTile；未找到则返回 null。</returns>
        public DataDefine.Tile.EventTile GetEventTileAtCell(Vector3Int cellPos)
        {
            return GetTileAtCell<DataDefine.Tile.EventTile>(cellPos, TileMapType.Event);
        }
        /// <summary>
        /// 获取指定单元格位置上的障碍图块（来自 TileMapType.Obstacle 图层）。
        /// </summary>
        /// <remarks>调用通用方法 GetTileAtCell T 并将图层类型指定为 TileMapType.Obstacle。</remarks>
        /// <param name="cellPos">要查询的瓦片单元格位置，使用 Vector3Int 表示的瓦片坐标。</param>
        /// <returns>找到则返回对应的 DataDefine.Tile.ObstacleTile；未找到则返回 null。</returns>
        public ObstacleTile GetObstacleTileAtCell(Vector3Int cellPos)
        {
            return GetTileAtCell<ObstacleTile>(cellPos, TileMapType.Obstacle);
        }
        /// <summary>
        /// 获取指定单元格位置上的地面图块（来自 TileMapType.Ground 图层）。
        /// </summary>
        /// <remarks>调用通用方法 GetTileAtCell T 并将图层类型指定为 TileMapType.Ground。</remarks>
        /// <param name="cellPos">要查询的瓦片单元格位置，使用 Vector3Int 表示的瓦片坐标。</param>
        /// <returns>找到则返回对应的 DataDefine.Tile.GroundTile；未找到则返回 null。</returns>
        public GroundTile GetGroundTileAtCell(Vector3Int cellPos)
        {
            return GetTileAtCell<GroundTile>(cellPos, TileMapType.Ground);
        }

        #endregion

        #region 移除

        /// <summary>
        /// 从指定单元格和图层移除瓦片并在成功时触发 OnEventTileRemoved 事件。
        /// </summary>
        /// <remarks>同时会从事件图层和目标图层清除对应瓦片；触发的 TileRemovedEventArgs 包含被移除的瓦片资源、单元格坐标及当前层
        /// ID（从全局事件变量获取）。</remarks>
        /// <param name="cellPos">要移除瓦片的网格单元格坐标。</param>
        /// <param name="tileMapType">要操作的瓦片图层类型（Ground、Obstacle、Event 等）。</param>
        /// <returns>成功移除瓦片返回 true；当地图未初始化、位置超出边界或目标图层不存在时返回 false。</returns>
        private bool RemoveTileAtCell(Vector3Int cellPos, TileMapType tileMapType)
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

        /// <summary>
        /// 从事件图层移除指定格位置的事件瓦片。
        /// </summary>
        /// <param name="cellPos">瓦片地图中的格子坐标，用于定位要移除的事件瓦片。</param>
        /// <returns>如果成功移除事件瓦片则返回 true；否则返回 false。</returns>
        public bool RemoveEventTileAtCell(Vector3Int cellPos)
        {
            return RemoveTileAtCell(cellPos, TileMapType.Event);
        }

        /// <summary>
        /// 从事件图层移除指定格位置的障碍瓦片。
        /// </summary>
        /// <param name="cellPos">瓦片地图中的格子坐标，用于定位要移除的障碍瓦片。</param>
        /// <returns>如果成功移除事件瓦片则返回 true；否则返回 false。</returns>
        public bool RemoveObstacleTileAtCell(Vector3Int cellPos)
        {
            return RemoveTileAtCell(cellPos, TileMapType.Obstacle);
        }

        /// <summary>
        /// 从事件图层移除指定格位置的地面瓦片。
        /// </summary>
        /// <param name="cellPos">瓦片地图中的格子坐标，用于定位要移除的地面瓦片。</param>
        /// <returns>如果成功移除事件瓦片则返回 true；否则返回 false。</returns>
        public bool RemoveGroundTileAtCell(Vector3Int cellPos)
        {
            return RemoveTileAtCell(cellPos, TileMapType.Ground);
        }


        #endregion

        #endregion

        #region 坐标与边界

        /// <summary>
        ///     将世界坐标转换为单元格坐标（带验证）。
        /// </summary>
        /// <param name="worldPos">输入的世界坐标</param>
        /// <param name="cellPos">输出的单元格坐标</param>
        /// <returns>转换是否成功（MapGrid或边界无效则返回 false）</returns>
        public bool TryWorldToCellPos(Vector2 worldPos, out Vector3Int cellPos)
        {
            cellPos = Vector3Int.zero;
            if (!mapGrid)
                return false;
            // 直接使用 Map.Grid 提供的 WorldToCell，然后验证边界
            cellPos = mapGrid.WorldToCell(worldPos);
            return IsInGridBounds(cellPos);
        }

        /// <summary>
        /// 判断给定的单元格坐标是否位于当前层的网格边界内。
        /// </summary>
        /// <remarks>若当前层边界无效，会记录警告并返回 false。</remarks>
        /// <param name="cellPos">要校验的单元格坐标（Vector3Int 格式）。</param>
        /// <returns>若当前层边界有效且坐标在边界内，则返回 true；否则返回 false。</returns>
        public bool IsInGridBounds(Vector3Int cellPos) // 边界检查重载1：单元格坐标判断
        {
            if (IsBoundsValid(_currentLayerBounds))
                return cellPos.x >= _currentLayerBounds.xMin && cellPos.x < _currentLayerBounds.xMax
                                                             && cellPos.y >= _currentLayerBounds.yMin &&
                                                             cellPos.y < _currentLayerBounds.yMax;
            Debug.LogWarning("IsInGridBounds:当前层边界非法");
            return false;
        }

        /// <summary>
        /// 判断给定的 BoundsInt 是否具有正的宽度和高度。
        /// </summary>
        /// <remarks>仅检查 size 的 x 和 y 分量；不验证 z 分量或其他属性。</remarks>
        /// <param name="bounds">要验证的 BoundsInt 实例。</param>
        /// <returns>当 bounds.size.x 和 bounds.size.y 均大于 0 时返回 true；否则返回 false。</returns>
        public static bool IsBoundsValid(BoundsInt bounds)
        {
            return bounds.size is { x: > 0, y: > 0 };
        }

        /// <summary>
        /// 返回指定格子位置在世界坐标系中的中心点坐标。
        /// </summary>
        /// <remarks>如果 mapGrid 为 null，会记录错误日志并返回 Vector2.zero。</remarks>
        /// <param name="cellPos">要获取中心点的格子坐标（Vector3Int）。</param>
        /// <returns>对应格子的中心点在世界坐标系中的位置；若内部的 mapGrid 为 null，则记录错误并返回 Vector2.zero。</returns>
        public Vector2 GetCellCenterWorld(Vector3Int cellPos)
        {
            if (mapGrid) return mapGrid.GetCellCenterWorld(cellPos);
            Debug.LogError("GetCellCenterWorld失败：MapGrid为null");
            return Vector2.zero;
        }

        /// <summary>
        ///     获取单元格原点（左下角）的世界坐标。部分系统（例如整数定位玩家）使用单元格原点作为对齐基准。
        /// </summary>
        /// <param name="cellPos">输入的单元格坐标。</param>
        /// <returns>单元格原点的世界坐标；若 mapGrid 为 null 则返回 Vector2.zero 并记录错误日志。</returns>
        public Vector2 GetCellOriginWorld(Vector3Int cellPos)
        {
            if (mapGrid) return mapGrid.CellToWorld(cellPos);
            Debug.LogError("GetCellOriginWorld失败：MapGrid为null");
            return Vector2.zero;
        }

        #endregion
    }
}