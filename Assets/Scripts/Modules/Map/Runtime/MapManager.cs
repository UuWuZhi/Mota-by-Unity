using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using VContainer;

/// <summary>
/// 地图管理系统（单例）：负责多楼层管理、楼层切换和数据缓存
/// </summary>
public class MapManager : MonoBehaviour
{
    [Header("地图配置")]
    public Grid mapRoot;                                               // 所有层的根Grid
    public int _currentLayerId = 0;                                    // 当前层数（默认0层）
    public List<MapLayerInfo> allLayers = new List<MapLayerInfo>();    // 所有层的配置

    // 缓存各层的Tilemap和边界（键：楼层ID，值：(ground, obstacle, eventTilemap, 边界)）
    private Dictionary<int, (Tilemap ground, Tilemap obstacle, Tilemap eventTilemap, BoundsInt bounds)> _layerDataCache =
        new Dictionary<int, (Tilemap, Tilemap, Tilemap, BoundsInt)>();

    private GridManager _gridManager;
    private EventCenter _eventCenter;
    private IGlobalEventVariables _globalEventVariables;

    //==============================================================================//
    //                                                                              //
    //                                 生命周期                                     //
    //                                                                              //
    //==============================================================================//
    #region 生命周期
    [Inject]
    public void Construct(GridManager gridManager, EventCenter eventCenter, IGlobalEventVariables globalEventVariables)
    {
        _gridManager = gridManager;
        _eventCenter = eventCenter;
        _globalEventVariables = globalEventVariables;
    }
    #endregion
    //==============================================================================//
    //                                                                              //
    //                                 事件处理                                     //
    //                                                                              //
    //==============================================================================//
    #region 事件处理
    /// <summary>
    /// 处理楼层切换请求（隐藏当前层，显示目标层，触发切换完成事件）
    /// </summary>
    public void RequestLayerSwitch(LayerSwitchRequestEventArgs switchArgs)
    {
        if (switchArgs == null || !_layerDataCache.ContainsKey(switchArgs.TargetLayerId))
        {
            Debug.LogError($"楼层{switchArgs?.TargetLayerId}不存在！");
            return;
        }

        // 1. 隐藏所有层
        foreach (var kvp in _layerDataCache)
        {
            kvp.Value.ground.gameObject.SetActive(false);
            kvp.Value.obstacle.gameObject.SetActive(false);
            kvp.Value.eventTilemap.gameObject.SetActive(false);
        }

        // 2. 激活目标层
        var targetData = _layerDataCache[switchArgs.TargetLayerId];
        Debug.Log($"切换到楼层{switchArgs.TargetLayerId}，激活Tilemaps");
        targetData.ground.gameObject.SetActive(true);
        targetData.obstacle.gameObject.SetActive(true);
        targetData.eventTilemap.gameObject.SetActive(true);

        // 3. 获取目标出生点位置
        Vector2 spawnPos = GetSpawnPosByLayerAndId(switchArgs.TargetLayerId, switchArgs.SpawnPointId);
        if (spawnPos == Vector2.zero)
        {
            Debug.LogError($"楼层{switchArgs.TargetLayerId}的出生点ID{switchArgs.SpawnPointId}无效！");
            return;
        }
        _currentLayerId = switchArgs.TargetLayerId;

        // 将当前楼层同步到全局事件变量存储
        if (_globalEventVariables != null)
        {
            _globalEventVariables.SetInt(GlobalEventKey.LayerId, _currentLayerId);
        }

        // 4. 发布楼层切换完成事件（通知玩家移动到出生点）
        if (_eventCenter != null)
        {
            _eventCenter.TriggerLayerSwitched(new LayerSwitchedEventArgs
            {
                GroundTilemap = targetData.ground,
                ObstacleTilemap = targetData.obstacle,
                EventTilemap = targetData.eventTilemap,
                LayerBounds = targetData.bounds,
                SpawnPos = spawnPos
            });
        }
        Debug.Log($"已切换到楼层{_currentLayerId}，出生点ID：{switchArgs.SpawnPointId}");
    }
    #endregion

    //==============================================================================//
    //                                                                              //
    //                                 地图初始化                                   //
    //                                                                              //
    //==============================================================================//
    #region 地图初始化
    /// <summary>
    /// 全局地图加载（游戏启动时调用）
    /// </summary>
    public void GlobalMapLoad()
    {
        CheckLayerIdDuplicate();   // 校验楼层ID唯一性
        SyncAllSpawnPoints();      // 验证出生点配置
        InitLayerDataCache();      // 初始化层数据缓存

        if (mapRoot == null)
            Debug.LogError("MapManager：mapRoot未配置！");
        if (_gridManager == null)
            Debug.LogError("MapManager：GridManager未注入！");

        // 同步当前层到全局变量存储
        if (_globalEventVariables != null)
        {
            _globalEventVariables.SetInt(GlobalEventKey.LayerId, _currentLayerId);
        }
    }

    /// <summary>
    /// 校验楼层ID是否重复
    /// </summary>
    private void CheckLayerIdDuplicate()
    {
        HashSet<int> idSet = new HashSet<int>();
        foreach (var layer in allLayers)
        {
            if (idSet.Contains(layer.layerId))
            {
                Debug.LogError($"楼层ID {layer.layerId} 重复！请检查配置");
            }
            idSet.Add(layer.layerId);
        }
    }

    /// <summary>
    /// 同步并验证所有楼层的出生点配置
    /// </summary>
    private void SyncAllSpawnPoints()
    {
        foreach (var layer in allLayers)
        {
            if (layer.layerRoot == null) continue;

            var layerInfo = layer.layerRoot.GetComponent<MapLayerInfo>();
            if (layerInfo != null && layerInfo.SpawnPoints.Count == 0)
            {
                Debug.LogWarning($"层 {layer.layerId} 无出生点配置");
            }
        }
    }

    /// <summary>
    /// 初始化层数据缓存（存储各层的Tilemap和边界）
    /// </summary>
    private void InitLayerDataCache()
    {
        foreach (var layerData in allLayers)
        {
            if (_layerDataCache.ContainsKey(layerData.layerId))
            {
                Debug.LogError($"层ID {layerData.layerId} 重复，已跳过初始化");
                continue;
            }
            if (layerData.layerRoot == null)
            {
                Debug.LogWarning($"层 {layerData.layerId} 的 layerRoot 为 null，跳过初始化");
                continue;
            }

            // 获取楼层信息组件
            MapLayerInfo layerInfo = layerData.layerRoot.GetComponent<MapLayerInfo>();
            if (layerInfo == null)
            {
                layerInfo = layerData.layerRoot.gameObject.AddComponent<MapLayerInfo>();
                Debug.LogWarning($"层 {layerData.layerId} 缺少MapLayerInfo组件，已自动添加");
            }

            // 查找地面、障碍和事件层 Tilemap
            Tilemap ground = layerData.layerRoot.Find("Ground")?.GetComponent<Tilemap>();
            Tilemap obstacle = layerData.layerRoot.Find("Obstacle")?.GetComponent<Tilemap>();
            Tilemap eventTilemap = layerData.layerRoot.Find("Event")?.GetComponent<Tilemap>();
            if (ground == null || obstacle == null || eventTilemap == null)
            {
                Debug.LogWarning($"层 {layerData.layerId} 缺少 Ground/Obstacle 或 Event Tilemap，跳过初始化");
                continue;
            }

            // 处理边界（优先使用预存边界，否则临时计算）
            BoundsInt bounds = _gridManager.IsBoundsValid(layerInfo.layerBounds)
                ? layerInfo.layerBounds
                : GetTilemapBounds(ground);

            // 存入缓存
            _layerDataCache.Add(layerData.layerId, (ground, obstacle, eventTilemap, bounds));

            // 初始禁用所有层（除了默认层，由切换事件激活）
            ground.gameObject.SetActive(false);
            obstacle.gameObject.SetActive(false);
            eventTilemap.gameObject.SetActive(false);
        }
    }
    #endregion

    //==============================================================================//
    //                                                                              //
    //                                 工具方法                                     //
    //                                                                              //
    //==============================================================================//
    #region 工具方法
    /// <summary>
    /// 计算Tilemap的边界（获取所有瓦片的最小/最大坐标）
    /// </summary>
    private BoundsInt GetTilemapBounds(Tilemap tilemap)
    {
        BoundsInt bounds = tilemap.cellBounds;
        return (bounds.size.x <= 0 || bounds.size.y <= 0)
            ? new BoundsInt(0, 0, 0, 1, 1, 1)
            : bounds;
    }
    public void GetLayerAndSpawnPosIDbyStairType(StairType stairType, out int layerId, out int spawnId)
    {
        switch (stairType)
        {
            case StairType.UpStair:
                layerId = GetUpperLayerId();
                spawnId = 0; // 上楼默认出生点ID
                break;
            case StairType.DownStair:
                layerId = GetLowerLayerId();
                spawnId = 1; // 下楼默认出生点ID
                break;
            default:
                layerId = 0;
                spawnId = 0;
                break;
        }
    }

    /// <summary>
    /// 根据楼层ID和出生点ID获取出生点坐标
    /// </summary>
    private Vector2 GetSpawnPosByLayerAndId(int layerId, int spawnId)
    {
        var layerInfo = allLayers.Find(l => l.layerId == layerId)?.layerRoot?.GetComponent<MapLayerInfo>();
        return layerInfo?.GetSpawnPointById(spawnId) ?? Vector2.zero;
    }

    /// <summary>
    /// 检查楼层是否存在
    /// </summary>
    public bool LayerExists(int layerId) => _layerDataCache.ContainsKey(layerId);

    /// <summary>
    /// 获取上一层ID（不存在返回0）
    /// </summary>
    public int GetUpperLayerId()
    {
        int targetId = _currentLayerId + 1;
        return LayerExists(targetId) ? targetId : 0;
    }

    /// <summary>
    /// 获取下一层ID（不存在返回0）
    /// </summary>
    public int GetLowerLayerId()
    {
        int targetId = _currentLayerId - 1;
        return LayerExists(targetId) ? targetId : 0;
    }

    /// <summary>
    /// 获取指定层的事件层Tilemap（供存读档使用）
    /// </summary>
    public Tilemap GetEventTilemapByLayer(int layerId)
    {
        return _layerDataCache.TryGetValue(layerId, out var data) ? data.eventTilemap : null;
    }

    /// <summary>
    /// 获取指定层的地面层Tilemap（供存读档使用）
    /// </summary>
    public Tilemap GetGroundTilemapByLayer(int layerId)
    {
        return _layerDataCache.TryGetValue(layerId, out var data) ? data.ground : null;
    }

    /// <summary>
    /// 获取指定层的障碍层Tilemap（供存读档使用）
    /// </summary>
    public Tilemap GetObstacleTilemapByLayer(int layerId)
    {
        return _layerDataCache.TryGetValue(layerId, out var data) ? data.obstacle : null;
    }
    #endregion
}