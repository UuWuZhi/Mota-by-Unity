// GridManager：基础网格管理系统
using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using System.Collections.Generic;
using VContainer;
using Unity.VisualScripting;

public class GridManager : MonoBehaviour
{
    public float tileSize = 1f;                 // 单个格子尺寸（和Tilemap的cell size一致）
    public Grid MapGrid;                        // 由外部注入，不再从MapManager获取
    private BoundsInt _currentLayerBounds;      // 缓存当前层边界
    private Tilemap _currentGroundTilemap;      // 当前层地面层引用
    private Tilemap _currentObstacleTilemap;    // 当前层障碍层引用
    private Tilemap _currentEventTilemap;       // 当前层事件层引用
    public Tilemap CurrentEventTilemap => _currentEventTilemap;

    private IGlobalEventVariables _globalEventVariables;
    private EventCenter _eventCenter;

    private bool _eventSubscribed = false;
    //==============================================================================//
    //                                                                              //
    //                                 生命周期                                     //
    //                                                                              //
    //==============================================================================//
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
    //==============================================================================//
    //                                                                              //
    //                                 事件系统                                     //
    //                                                                              //
    //==============================================================================//
    #region 事件系统
    private void OnLayerSwitchedHandler(object sender, System.EventArgs args)
    {
        LayerSwitchedEventArgs layerArgs = args as LayerSwitchedEventArgs;
        if (layerArgs == null || layerArgs.GroundTilemap == null || layerArgs.ObstacleTilemap == null || layerArgs.EventTilemap == null)
        {
            Debug.LogError("GridManager：楼层切换事件数据无效！");
            return;
        }
        // 更新当前层数据
        UpdateCurrentLayerData(layerArgs.GroundTilemap, layerArgs.ObstacleTilemap, layerArgs.EventTilemap, layerArgs.LayerBounds);
    }
    private void SubscribeEventCenter()
    {
        if (_eventCenter == null || _eventSubscribed) return;
        _eventCenter.OnLayerSwitched += OnLayerSwitchedHandler;
        _eventSubscribed = true;
    }
    private void UnsubscribeEventCenter()
    {
        if (_eventCenter == null || !_eventSubscribed) return;
        _eventCenter.OnLayerSwitched -= OnLayerSwitchedHandler;
        _eventSubscribed = false;
    }
    #endregion
    //==============================================================================//
    //                                                                              //
    //                                 地图加载                                     //
    //                                                                              //
    //==============================================================================//
    #region 地图加载
    // 更新数据至当前层
    public void UpdateCurrentLayerData(Tilemap groundTilemap, Tilemap obstacleTilemap, Tilemap eventTilemap, BoundsInt layerBounds)
    {
        _currentGroundTilemap = groundTilemap;
        _currentObstacleTilemap = obstacleTilemap;
        _currentEventTilemap = eventTilemap;
        _currentLayerBounds = layerBounds; // 直接使用传入的边界
        LoadMap(); // 重新加载当前层的网格数据
    }
    //加载地图数据
    public void LoadMap() 
    {
        if (MapGrid == null)
        {
            Debug.LogError("LoadMap失败：MapGrid未初始化（请先调用SetMapGrid）");
            return;
        }
        if (_currentGroundTilemap == null || _currentObstacleTilemap == null || _currentEventTilemap == null)
        {
            Debug.LogError("LoadMap失败：当前层Tilemap为null");
            return;
        }
        if (!IsBoundsValid(_currentLayerBounds))
        {
            Debug.LogError("LoadMap失败：层边界无效:{_currentLayerBounds}");
            return;
        }
        // 不再在内存中缓存基础层格子类型。读取时按需直接查询对应 Tilemap。
        // Debug.Log("LoadMap: 当前层 Tilemap 已设置（Ground/Obstacle/Event），按需查询 Tilemap");
    }

    #endregion
    //==============================================================================//
    //                                                                              //
    //                                 瓦片操作                                     //
    //                                                                              //
    //==============================================================================//
    #region 瓦片操作
    //==============================================================================//
    //                                 操作：查询                                   //
    //==============================================================================//
    #region 查询
    /// <summary>
    /// 直接查询指定世界坐标在当前层指定图层上的 Tile
    /// </summary>
    public TileBase GetTileAtWorldPos(Vector2 worldPos, TileMapType tileMapType)
    {
        if (MapGrid == null) return null;
        Vector3Int cellPos = MapGrid.WorldToCell(worldPos);   
        return GetTileAtCell(cellPos, tileMapType);
    }
    public TileBase GetTileAtCell(Vector3Int cellPos, TileMapType tileMapType)
    {
        if (MapGrid == null) return null;
        if (!IsInGridBounds(cellPos)) return null;
        return tileMapType switch
        {
            TileMapType.Ground => _currentGroundTilemap.GetTile(cellPos),
            TileMapType.Obstacle => _currentObstacleTilemap.GetTile(cellPos),
            TileMapType.Event => _currentEventTilemap.GetTile(cellPos),
            _ => null,
        };
    }
    /// <summary>
    /// 泛型重载：根据 TileMapType 返回指定派生类型的 Tile（若类型不匹配返回 null）
    /// </summary>
    public T GetTileAtWorldPos<T>(Vector2 worldPos, TileMapType tileMapType) where T : TileBase
    {
        TileBase tile = GetTileAtWorldPos(worldPos, tileMapType);
        return tile as T;
    }
    public T GetTileAtCell<T>(Vector3Int cellPos, TileMapType tileMapType) where T : TileBase
    {
        TileBase tile = GetTileAtCell(cellPos, tileMapType);
        return tile as T;
    }
    public EventTile GetEventTileAtCell(Vector3Int cellPos){ return GetTileAtCell<EventTile>(cellPos, TileMapType.Event); }
    public EventTile GetEventTileAtWorldPos(Vector2 worldPos){ return GetTileAtWorldPos<EventTile>(worldPos, TileMapType.Event); }
    public ObstacleTile GetObstacleTileAtCell(Vector3Int cellPos){ return GetTileAtCell<ObstacleTile>(cellPos, TileMapType.Obstacle); }
    public ObstacleTile GetObstacleTileAtWorldPos(Vector2 worldPos){ return GetTileAtWorldPos<ObstacleTile>(worldPos, TileMapType.Obstacle); }
    public GroundTile GetGroundTileAtCell(Vector3Int cellPos){ return GetTileAtCell<GroundTile>(cellPos, TileMapType.Ground); }
    public GroundTile GetGroundTileAtWorldPos(Vector2 worldPos){ return GetTileAtWorldPos<GroundTile>(worldPos, TileMapType.Ground); }
    #endregion
    //==============================================================================//
    //                                 操作：移除                                   //
    //==============================================================================//
    #region 移除
    public bool RemoveTileAtCell(Vector3Int cellPos, TileMapType tileMapType)
    {
        if (MapGrid == null) return false;
        if (!IsInGridBounds(cellPos)) return false;
        Tilemap targetTilemap = tileMapType switch
        {
            TileMapType.Ground => _currentGroundTilemap,
            TileMapType.Obstacle => _currentObstacleTilemap,
            TileMapType.Event => _currentEventTilemap,
            _ => null,
        };
        if (targetTilemap == null) return false;
        BaseTile removed = _currentEventTilemap.GetTile(cellPos) as BaseTile;
        _currentEventTilemap.SetTile(cellPos, null);

        int layerId = _globalEventVariables.GetInt(GlobalEventKey.LayerId);
        targetTilemap.SetTile(cellPos, null);
        _eventCenter.TriggerEventTileRemoved(new TileRemovedEventArgs
        {
            Cell = cellPos,
            LayerId = layerId,
            TileAsset = removed
        });
        return true;
    }
    public bool RemoveTileAtWorldPos(Vector2 worldPos, TileMapType tileMapType)
    {
        if (MapGrid == null) return false;
        Vector3Int cellPos = MapGrid.WorldToCell(worldPos);
        return RemoveTileAtCell(cellPos, tileMapType);
    }
    public bool RemoveEventTileAtCell(Vector3Int cellPos) { return RemoveTileAtCell(cellPos, TileMapType.Event); }
    public bool RemoveEventTileAtWorldPos(Vector2 worldPos) { return RemoveTileAtWorldPos(worldPos, TileMapType.Event); }
    public bool RemoveObstacleTileAtCell(Vector3Int cellPos) { return RemoveTileAtCell(cellPos, TileMapType.Obstacle); }
    public bool RemoveObstacleTileAtWorldPos(Vector2 worldPos) { return RemoveTileAtWorldPos(worldPos, TileMapType.Obstacle); }
    public bool RemoveGroundTileAtCell(Vector3Int cellPos) { return RemoveTileAtCell(cellPos, TileMapType.Ground); }
    public bool RemoveGroundTileAtWorldPos(Vector2 worldPos) { return RemoveTileAtWorldPos(worldPos, TileMapType.Ground); }
    #endregion
    #endregion
    //==============================================================================//
    //                                                                              //
    //                                 坐标与边界                                   //
    //                                                                              //
    //==============================================================================//
    #region 坐标与边界
    public bool TryConvertWorldToGridPos(Vector2 worldPos, out int gridX, out int gridY) // 世界坐标转网格坐标（带验证）
    {
        gridX = -1;
        gridY = -1;
        if (MapGrid == null)
            return false;
        Vector3Int cellPos = MapGrid.WorldToCell(worldPos);
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
        int cellX = gridX + _currentLayerBounds.xMin;
        int cellY = gridY + _currentLayerBounds.yMin;
        Vector3Int cellPos = new Vector3Int(cellX, cellY, 0);
        // 单元格坐标 → 世界坐标（中心位置）
        worldPos = MapGrid.GetCellCenterWorld(cellPos);
        return true;
    }
    
    public bool IsInGridBounds(Vector3Int cellPos) // 边界检查重载1：单元格坐标判断
    {
        if (!IsBoundsValid(_currentLayerBounds))
        {
            Debug.LogWarning($"IsInGridBounds:当前层边界非法");
            return false;
        }
        return cellPos.x >= _currentLayerBounds.xMin && cellPos.x < _currentLayerBounds.xMax
            && cellPos.y >= _currentLayerBounds.yMin && cellPos.y < _currentLayerBounds.yMax;
    }

    public bool IsInGridBounds(int gridX, int gridY) // 边界检查重载2：网格坐标判断
    {
        if (!IsBoundsValid(_currentLayerBounds))
        {
            Debug.LogWarning($"IsInGridBounds:当前层边界非法");
            return false;
        }
        int width = _currentLayerBounds.size.x;
        int height = _currentLayerBounds.size.y;
        return gridX >= 0 && gridX < width && gridY >= 0 && gridY < height;
    }

    public bool IsBoundsValid(BoundsInt bounds) //边界检查基础：边界合法
    {
        return bounds.size.x > 0 && bounds.size.y > 0;
    }
    
    public Vector2 GetCellCenterWorld(Vector2 worldPos) //获取格子中心世界坐标
    {
        if (MapGrid == null)
        {
            Debug.LogError("GetCellCenterWorld失败：MapGrid为null");
            return worldPos;
        }
        Vector3Int cellPos = MapGrid.WorldToCell(worldPos);
        return MapGrid.GetCellCenterWorld(cellPos);
    }

    // 新增重载：直接使用单元格坐标获取格子中心世界坐标，统一入口
    public Vector2 GetCellCenterWorld(Vector3Int cellPos)
    {
        if (MapGrid == null)
        {
            Debug.LogError("GetCellCenterWorld失败：MapGrid为null");
            return Vector2.zero;
        }
        return MapGrid.GetCellCenterWorld(cellPos);
    }
    
    private BoundsInt GetMapTotalBounds(Tilemap tilemap) //用于计算单个地图的边界（不知道有啥用，先留着吧）
    {
        BoundsInt tilemapBounds = tilemap.cellBounds;
        if (tilemapBounds.size.x <= 0 || tilemapBounds.size.y <= 0)
        {
            return new BoundsInt(0, 0, 0, 1, 1, 1); // 默认空边界
        }
        return tilemapBounds;
    }
    
    #endregion
}