// GridManager：基础网格管理系统
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;
    public float tileSize = 1f; // 单个格子尺寸（和Tilemap的cell size一致）
    public Grid MapGrid; // 由外部注入，不再从MapManager获取
    private GridType[,] _baseGridData;  // 基础层格子类型存储
    private BoundsInt _currentLayerBounds; // 缓存当前层边界
    private Tilemap _currentGroundWallTilemap; //当前层基础层引用
    private Tilemap _currentEventTilemap; //当前层事件层引用
    public Tilemap CurrentEventTilemap => _currentEventTilemap;
    //==============================================================================//
    //                                                                              //
    //                                 生命周期                                     //
    //                                                                              //
    //==============================================================================//
    #region 生命周期
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 确保跨场景存在
        }
        else
        {
            Debug.LogWarning($"重复创建GridManager实例，已销毁多余实例：{gameObject.name}");
            Destroy(gameObject);
        }
    }
    private void OnEnable()
    {
        if (EventCenter.Instance != null)
        {
            EventCenter.Instance.OnLayerSwitched += OnLayerSwitchedHandler; //层数切换确认
        }
        else
        {
            Debug.LogError("GridManager启用失败：EventCenter.Instance为null");
        }
    }

    private void OnDisable()
    {
        if (EventCenter.Instance != null)
        {
            EventCenter.Instance.OnLayerSwitched -= OnLayerSwitchedHandler;
        }
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
        if (layerArgs == null || layerArgs.GroundWallTilemap == null || layerArgs.EventTilemap == null)
        {
            Debug.LogError("GridManager：楼层切换事件数据无效！");
            return;
        }
        // 更新当前层数据
        UpdateCurrentLayerData(layerArgs.GroundWallTilemap, layerArgs.EventTilemap, layerArgs.LayerBounds);
    }
    #endregion
    //==============================================================================//
    //                                                                              //
    //                                 地图加载                                     //
    //                                                                              //
    //==============================================================================//
    #region 地图加载
    // 更新数据至当前层
    public void UpdateCurrentLayerData(Tilemap groundWallTilemap, Tilemap eventTilemap, BoundsInt layerBounds) 
    {
        _currentGroundWallTilemap = groundWallTilemap;
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
        if (_currentGroundWallTilemap == null || _currentEventTilemap == null)
        {
            Debug.LogError("LoadMap失败：当前层Tilemap为null");
            return;
        }
        if (!IsBoundsValid(_currentLayerBounds))
        {
            Debug.LogError("LoadMap失败：层边界无效:{_currentLayerBounds}");
            return;
        }
        // 获取地图的宽和高（根据所有Tilemap的最大范围计算）
        int width = _currentLayerBounds.size.x;
        int height = _currentLayerBounds.size.y;
        _baseGridData = new GridType[width, height];
        
        for (int x = 0; x < width; x++) // 初始化所有格子
        {
            for (int y = 0; y < height; y++)
            {
                _baseGridData[x, y] = GridType.Wall;
            }
        }
        Debug.Log("LoadMap:格子类型初始化完成");
        UpdateGridDataByTilemap(_currentGroundWallTilemap); // 加载基础层数据
    }
    //读取传入的TileMap，并根据类型更新瓦片数据
    private void UpdateGridDataByTilemap(Tilemap tilemap) 
    {
        if (tilemap == null)
        {
            Debug.LogWarning("UpdateGridDataByTilemap：传入的Tilemap为null");
            return;
        }
        // 获取Tilemap中所有有瓦片的位置（本地网格坐标）
        foreach (Vector3Int cellPos in tilemap.cellBounds.allPositionsWithin)
        {
            if (!tilemap.HasTile(cellPos)) // 该位置无瓦片
                continue;

            // 转换为全局网格坐标（相对于地图Grid）
            if (!TryConvertCellToGridPos(cellPos, out int gridX, out int gridY))
            {
                Debug.LogWarning($"瓦片位置 {cellPos} 超出当前层边界，已忽略");
                continue;
            }

            if (tilemap.tag == "GroundWall")
            {
                if (tilemap.GetTile(cellPos) is GroundWallTile groundWallTile)
                {
                    _baseGridData[gridX, gridY] = groundWallTile.tileType; // 直接使用瓦片定义的类型
                }
            }
        }
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
    //根据世界坐标与图层类型获取对应瓦片类型
    public GridType GetGridTypeByWorldPos(Vector2 targetworldPos, TileMapType tileMapType)
    {
        if (tileMapType == TileMapType.GroundWall)
        {
            if (!TryConvertWorldToGridPos(targetworldPos, out int gridX, out int gridY))
                return GridType.Wall;
            return _baseGridData[gridX, gridY];
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

        int layerId = GlobalEventVariables.Instance.LayerId;

        // 通知系统瓦片已移动
        EventCenter.Instance?.TriggerEventTileMoved(new TileMovedEventArgs
        {
            FromCell = fromCell,
            ToCell = toCell,
            LayerId = layerId,
            TileAsset = sourceTile
        });

        // Update EventNodeManager registration if any Mono existed at cell
        if (EventNodeManager.Instance != null)
        {
            if (EventNodeManager.Instance.TryGetEventNodeAtCell(fromCell, out var node) && node != null)
            {
                EventNodeManager.Instance.UnregisterEventNodeAtCell(fromCell);
                EventNodeManager.Instance.RegisterEventNodeAtCell(toCell, node);
                node.CellPos = toCell;
                node.transform.position = MapGrid.GetCellCenterWorld(toCell);
            }
        }

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
        if (tileMapType == TileMapType.GroundWall)
        {
            if (!IsInGridBounds(gridX, gridY))
            {
                Debug.LogWarning($"更新网格类型失败：坐标 ({gridX},{gridY}) 超出边界");
                return;
            }
            _baseGridData[gridX, gridY] = newType;
            return;
        }
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
        _currentEventTilemap.SetTile(cellPos, null);

        // If there is an EventNode Mono at this cell, unregister and destroy it
        if (EventNodeManager.Instance != null && EventNodeManager.Instance.TryGetEventNodeAtCell(cellPos, out var node) && node != null)
        {
            EventNodeManager.Instance.UnregisterEventNodeAtCell(cellPos);
            GameObject.Destroy(node.gameObject);
        }
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
        if (_baseGridData == null)
        {
            Debug.LogWarning($"IsInGridBounds:当前层基础层数据为空");
            return false;
        }
        return gridX >= 0 && gridX < _baseGridData.GetLength(0)
            && gridY >= 0 && gridY < _baseGridData.GetLength(1);
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