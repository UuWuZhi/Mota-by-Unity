// GridManager：基础网格管理系统
using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using System.Collections.Generic;
using VContainer;

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
    /// <summary>
    /// 直接查询指定世界坐标在当前层指定图层上的 Tile
    /// </summary>
    public TileBase GetTileAtWorldPos(Vector2 worldPos, TileMapType tileMapType)
    {
        if (MapGrid == null) return null;
        Vector3Int cellPos = MapGrid.WorldToCell(worldPos);

        // 检查是否在当前层边界内
        if (!IsInGridBounds(cellPos)) return null;

        return tileMapType switch
        {
            TileMapType.Ground => _currentGroundTilemap.GetTile(cellPos),
            TileMapType.Obstacle => _currentObstacleTilemap.GetTile(cellPos),
            TileMapType.Event => _currentEventTilemap.GetTile(cellPos),
            _ => null,
        };
    }
    //根据世界坐标与图层类型获取对应瓦片类型
    public GridType GetGridTypeByWorldPos(Vector2 targetworldPos, TileMapType tileMapType)
    {
        // 通过直接查询 Tilemap 判断类型：Ground 层有瓦片则视为 Ground，可通行；无瓦片视为 Wall（不可通行）
        if (!TryConvertWorldToGridPos(targetworldPos, out int gridX, out int gridY))
            return GridType.Wall;

        TileBase tile = GetTileAtWorldPos(targetworldPos, tileMapType);
        if (tileMapType == TileMapType.Ground)
        {
            return tile != null ? GridType.Ground : GridType.Wall;
        }
        else if (tileMapType == TileMapType.Obstacle)
        {
            return tile != null ? GridType.Wall : GridType.Ground;
        }
        return GridType.None;
    }
    //==============================================================================//
    //                                 操作：更新                                   //
    //==============================================================================//
    // 新增：移动事件层瓦片（单元格坐标）
    public bool MoveEventTile(Vector3Int fromCell, Vector3Int toCell, bool overwrite = false)
    {
        if (MapGrid == null || _currentEventTilemap == null)
        {
            Debug.LogError("MoveEventTile失败：MapGrid或EventTilemap为null");
            return false;
        }

        if (fromCell == toCell) return false;

        // 检查源格是否有瓦片
        if (!_currentEventTilemap.HasTile(fromCell))
        {
            Debug.LogWarning($"MoveEventTile: 源格无瓦片 {fromCell}");
            return false;
        }

        // 目标存在且不覆盖时拒绝
        if (_currentEventTilemap.HasTile(toCell) && !overwrite)
        {
            Debug.LogWarning($"MoveEventTile: 目标格已有瓦片且 overwrite=false，目标：{toCell}");
            return false;
        }

        // 获取源瓦片（引用）
        EventTile sourceTile = _currentEventTilemap.GetTile(fromCell) as EventTile;
        if (sourceTile == null)
        {
            Debug.LogWarning($"MoveEventTile: 源瓦片类型非 EventTile 或为空 at {fromCell}");
            return false;
        }

        // 执行 Tilemap 操作：在目标放置源引用，并移除源位
        _currentEventTilemap.SetTile(toCell, sourceTile);
        _currentEventTilemap.SetTile(fromCell, null);

        // 不再维护 _eventGridData；EventNode / Tile assets 表示事件类型

        // 同步实体（若存在）：告诉 EntityManager 更新键与位置

        int layerId = _globalEventVariables.GetInt(GlobalEventKey.LayerId);

        // 通知系统瓦片已移动
        _eventCenter.TriggerEventTileMoved(new TileMovedEventArgs
        {
            FromCell = fromCell,
            ToCell = toCell,
            LayerId = layerId,
            TileAsset = sourceTile
        });

        // EventNodeManager will handle updating registrations when it receives the TileMoved event

        return true;
    }

    // 新增：使用世界坐标版本（方便外部调用）
    public bool MoveEventTile(Vector2 fromWorldPos, Vector2 toWorldPos, bool overwrite = false)
    {
        if (MapGrid == null) return false;
        Vector3Int fromCell = MapGrid.WorldToCell(fromWorldPos);
        Vector3Int toCell = MapGrid.WorldToCell(toWorldPos);
        return MoveEventTile(fromCell, toCell, overwrite);
    }
    //根据世界坐标与图层类型更新对应瓦片
    public void UpdateGridType(int gridX, int gridY, GridType newType, TileMapType tileMapType)
    {
        // 现在不维护内存中的基础层缓存。如需修改 Tilemap，请使用外部直接操作 Tilemap API。
        Debug.LogWarning("UpdateGridType: 已弃用内存缓存，若需修改 Tilemap 请直接使用 Tilemap.SetTile");
    }

    public void RemoveEventTile(Vector2 worldPos) // 重载1：使用世界坐标移除事件层图块
    {
        if (MapGrid == null || _currentEventTilemap == null)
            return;
        Vector3Int cellPos = MapGrid.WorldToCell(worldPos);
        RemoveEventTile(cellPos);
    }
    public void RemoveEventTile(Vector3Int cellPos) // 重载2：使用网格坐标移除事件层图块
    {
        if (MapGrid == null || _currentEventTilemap == null)
            return;
        // 获取当前瓦片引用以放入事件参数
        EventTile removed = _currentEventTilemap.GetTile(cellPos) as EventTile;
        _currentEventTilemap.SetTile(cellPos, null);

        int layerId = _globalEventVariables.GetInt(GlobalEventKey.LayerId);
        // 触发事件层瓦片移除事件
        _eventCenter.TriggerEventTileRemoved(new TileRemovedEventArgs
        {
            Cell = cellPos,
            LayerId = layerId,
            TileAsset = removed
        });

        // 不直接处理 EventNodeManager 的注销/销毁，交由 EventNodeManager 监听 TileRemoved 事件处理
    }
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