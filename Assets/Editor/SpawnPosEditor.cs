using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 楼梯出生点自动生成工具（Editor）
/// 功能：识别每层Event层的上下楼梯Tile，自动生成/更新对应SpawnPoint
/// - ID=0：从下层上来的出生点（对应DownStair位置）
/// - ID=1：从上层下来的出生点（对应UpStair位置）
/// </summary>
[CustomEditor(typeof(MapManager))]
public class MapSpawnPointAutoGenerator : Editor
{
    private MapManager _mapManager;
    private const int SpawnId_UpStair = 0;    // 上楼梯出生点ID（从下层上来）
    private const int SpawnId_DownStair = 1;  // 下楼梯出生点ID（从上层下来）

    private void OnEnable()
    {
        _mapManager = target as MapManager;
    }

    public override void OnInspectorGUI()
    {
        // 先绘制默认Inspector（保留原有参数）
        DrawDefaultInspector();

        if (_mapManager == null)
        {
            EditorGUILayout.HelpBox("⚠️ 未绑定MapManager实例", MessageType.Error);
            return;
        }

        // 分割线 + 标题
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("=== 楼梯出生点自动生成工具 ===", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 1. 批量生成所有楼层的楼梯出生点
        if (GUILayout.Button("🚀 批量生成所有楼层楼梯出生点", GUILayout.Height(35)))
        {
            BatchGenerateSpawnPoints();
        }

        // 2. 单独生成指定楼层的出生点
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("单独生成指定楼层：");
        
        // 构建楼层下拉选项
        List<string> layerOptions = new List<string> { "请选择楼层" };
        List<int> layerIds = new List<int> { -1 };
        foreach (var layerData in _mapManager.allLayers)
        {
            layerOptions.Add($"楼层 {layerData.layerId} ({layerData.layerRoot?.name ?? "未配置根节点"})");
            layerIds.Add(layerData.layerId);
        }

        // 下拉选择框
        int selectedIdx = EditorGUILayout.Popup("目标楼层", 0, layerOptions.ToArray());
        if (selectedIdx > 0 && GUILayout.Button("🔄 生成选中楼层出生点"))
        {
            int targetLayerId = layerIds[selectedIdx];
            GenerateSpawnPointsForSingleLayer(targetLayerId);
        }

        // 说明提示
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "📝 规则说明：\n" +
            "1. ID=0：绑定DownStair（下楼梯）位置（从下层上来的出生点）\n" +
            "2. ID=1：绑定UpStair（上楼梯）位置（从上层下来的出生点）\n" +
            "3. 未找到楼梯时，生成默认位置（楼层根节点坐标）\n" +
            "4. 已存在的SpawnPoint会自动更新坐标，不会重复创建", 
            MessageType.Info
        );
    }

    #region 批量生成逻辑
    /// <summary>
    /// 批量为所有楼层生成楼梯出生点
    /// </summary>
    private void BatchGenerateSpawnPoints()
    {
        if (_mapManager.allLayers.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "未配置任何楼层数据！", "确定");
            return;
        }

        int successCount = 0;
        int failCount = 0;
        List<string> failLogs = new List<string>();

        // 遍历所有楼层
        foreach (var layerData in _mapManager.allLayers)
        {
            if (GenerateSpawnPointsForLayer(layerData, out string errorMsg))
            {
                successCount++;
            }
            else
            {
                failCount++;
                failLogs.Add($"楼层 {layerData.layerId}：{errorMsg}");
            }
        }

        // 生成结果提示
        string resultMsg = $"批量生成完成！\n✅ 成功：{successCount} 层\n❌ 失败：{failCount} 层";
        if (failLogs.Count > 0)
        {
            resultMsg += $"\n\n失败详情：\n{string.Join("\n", failLogs)}";
        }
        EditorUtility.DisplayDialog("生成结果", resultMsg, "确定");

        // 刷新场景视图
        SceneView.RepaintAll();
    }
    #endregion

    #region 单个楼层生成逻辑
    /// <summary>
    /// 为指定楼层ID生成出生点
    /// </summary>
    private void GenerateSpawnPointsForSingleLayer(int targetLayerId)
    {
        var layerData = _mapManager.allLayers.FirstOrDefault(l => l.layerId == targetLayerId);
        if (layerData == null)
        {
            EditorUtility.DisplayDialog("错误", $"未找到楼层ID：{targetLayerId}", "确定");
            return;
        }

        if (GenerateSpawnPointsForLayer(layerData, out string errorMsg))
        {
            EditorUtility.DisplayDialog("成功", $"楼层 {targetLayerId} 出生点生成完成！", "确定");
        }
        else
        {
            EditorUtility.DisplayDialog("失败", $"楼层 {targetLayerId} 生成失败：{errorMsg}", "确定");
        }

        SceneView.RepaintAll();
    }

    /// <summary>
    /// 核心逻辑：为单个楼层生成/更新楼梯出生点
    /// </summary>
    private bool GenerateSpawnPointsForLayer(MapLayerInfo layerData, out string errorMsg)
    {
        errorMsg = string.Empty;

        // 校验1：楼层根节点是否配置
        if (layerData.layerRoot == null)
        {
            errorMsg = "楼层根节点（layerRoot）未配置";
            return false;
        }

        // 校验2：获取/创建MapLayerInfo组件
        MapLayerInfo layerInfo = layerData.layerRoot.GetComponent<MapLayerInfo>();
        if (layerInfo == null)
        {
            layerInfo = layerData.layerRoot.gameObject.AddComponent<MapLayerInfo>();
            Debug.LogWarning($"楼层 {layerData.layerId} 缺少MapLayerInfo，已自动创建");
        }

        // 校验3：查找Event Tilemap（事件层，包含楼梯Tile）
        Transform eventTrans = layerData.layerRoot.Find("Event");
        if (eventTrans == null)
        {
            errorMsg = "未找到Event子节点（事件层容器）";
            return false;
        }
        Tilemap eventTilemap = eventTrans.GetComponent<Tilemap>();
        if (eventTilemap == null)
        {
            errorMsg = "Event节点未挂载Tilemap组件";
            return false;
        }

        // 步骤1：查找上下楼梯的世界坐标
        Vector2 upStairPos = FindStairWorldPos(eventTilemap, StairType.UpStair);
        Vector2 downStairPos = FindStairWorldPos(eventTilemap, StairType.DownStair);

        // 步骤2：初始化SpawnPoints列表（确保不为null）
        if (layerInfo.SpawnPoints == null)
        {
            layerInfo.SpawnPoints = new List<MapLayerInfo.SpawnPoint>();
        }
        Vector2 defaultRootPos = new Vector2(layerData.layerRoot.position.x, layerData.layerRoot.position.y);
        
        // 步骤3：更新/添加上楼梯出生点（ID=0，对应下楼梯位置）
        UpdateSpawnPoint(layerInfo, SpawnId_UpStair, downStairPos, defaultRootPos);
        // 步骤4：更新/添加下楼梯出生点（ID=1，对应上楼梯位置）
        UpdateSpawnPoint(layerInfo, SpawnId_DownStair, upStairPos, defaultRootPos);

        // 步骤5：同步楼层ID和根节点
        Undo.RecordObject(layerInfo, $"Generate SpawnPoints for Layer {layerData.layerId}");
        layerInfo.layerId = layerData.layerId;
        layerInfo.layerRoot = layerData.layerRoot;
        layerInfo.spawnTilemap = eventTilemap; // 绑定事件层Tilemap

        // 标记为已修改（确保保存）
        EditorUtility.SetDirty(layerInfo);
        EditorUtility.SetDirty(_mapManager);

        // 日志提示
        string logMsg = $"楼层 {layerData.layerId} 出生点更新：\n";
        logMsg += downStairPos == Vector2.zero  
            ? "  - 上楼梯出生点（ID=0）：未找到下楼梯，使用默认位置\n" 
            : $"  - 上楼梯出生点（ID=0）：{downStairPos}\n";
        logMsg += upStairPos == Vector2.zero
            ? "  - 下楼梯出生点（ID=1）：未找到上楼梯，使用默认位置\n" 
            : $"  - 下楼梯出生点（ID=1）：{upStairPos}";
        Debug.Log(logMsg);

        return true;
    }
    #endregion

    #region 辅助方法
    /// <summary>
    /// 查找指定类型楼梯的世界坐标
    /// </summary>
    private Vector2 FindStairWorldPos(Tilemap eventTilemap, StairType stairType)
    {
        // 遍历Event层所有有Tile的位置
        foreach (Vector3Int cellPos in eventTilemap.cellBounds.allPositionsWithin)
        {
            if (!eventTilemap.HasTile(cellPos)) continue;

            // 检查Tile是否为EventTile且类型是楼梯
            if (eventTilemap.GetTile(cellPos) is EventTile eventTile)
            {
                if (eventTile.tileType == GridType.Stair)
                {
                    StairTile stairTile = eventTile as StairTile;
                    if (stairTile.stairType == stairType)
                    {
                        Vector3 worldPos = eventTilemap.CellToWorld(cellPos);
                        return new Vector2(worldPos.x, worldPos.y);
                    }
                }
            }
        }

        // 未找到时返回默认值（楼层根节点坐标）
        return Vector2.zero;
    }

    /// <summary>
    /// 更新/添加SpawnPoint（存在则更新坐标，不存在则创建）
    /// </summary>
    private void UpdateSpawnPoint(MapLayerInfo layerInfo, int spawnId, Vector2 targetPos, Vector2 defaultPos)
    {
        // 若目标坐标为空，使用默认位置（楼层根节点）
        Vector2 finalPos = targetPos == Vector2.zero ? defaultPos : targetPos;

        // 查找已有SpawnPoint
        var existingSpawn = layerInfo.SpawnPoints.FirstOrDefault(p => p.id == spawnId);
        if (existingSpawn != null)
        {
            // 更新已有SpawnPoint的坐标
            existingSpawn.position = finalPos;
        }
        else
        {
            // 创建新的SpawnPoint
            layerInfo.SpawnPoints.Add(new MapLayerInfo.SpawnPoint
            {
                id = spawnId,
                position = finalPos
            });
        }
    }
    #endregion
}