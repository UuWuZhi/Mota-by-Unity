// using UnityEngine;
// using UnityEditor;
// using UnityEngine.Tilemaps;
// using System.Collections.Generic;

// /// <summary>
// /// 地图楼层边界自动计算工具（Editor）
// /// 功能：批量/单独计算各楼层的GroundWall层边界，写入MapLayerInfo的layerBounds字段
// /// </summary>
// [CustomEditor(typeof(MapManager))] // 扩展MapManager的Inspector面板
// public class MapLayerBoundsEditor : Editor
// {
//     private MapManager _targetMapManager; // 当前编辑的MapManager实例

//     private void OnEnable()
//     {
//         // 获取当前选中的MapManager组件
//         _targetMapManager = target as MapManager;
//     }

//     /// <summary>
//     /// 重写Inspector绘制逻辑（添加自定义按钮）
//     /// </summary>
//     public override void OnInspectorGUI()
//     {
//         // 先绘制默认的Inspector面板（保留原有参数）
//         DrawDefaultInspector();

//         if (_targetMapManager == null)
//         {
//             EditorGUILayout.HelpBox("未找到MapManager实例！", MessageType.Error);
//             return;
//         }

//         // 分割线
//         EditorGUILayout.Space();
//         EditorGUILayout.LabelField("=== 楼层边界计算工具 ===", EditorStyles.boldLabel);
//         EditorGUILayout.Space();

//         // 1. 批量计算所有楼层边界按钮
//         if (GUILayout.Button("📊 批量计算所有楼层边界", GUILayout.Height(30)))
//         {
//             CalculateAllLayersBounds();
//         }

//         // 2. 单独计算选中楼层边界（下拉选择）
//         EditorGUILayout.Space();
//         EditorGUILayout.LabelField("单独更新指定楼层：");

//         // 构建楼层下拉选项
//         List<string> layerOptions = new List<string>();
//         List<int> layerIds = new List<int>();
//         layerOptions.Add("请选择楼层");
//         layerIds.Add(-1);

//         foreach (var layerData in _targetMapManager.allLayers)
//         {
//             layerOptions.Add($"楼层 {layerData.layerId} ({layerData.layerRoot?.name ?? "未配置根节点"})");
//             layerIds.Add(layerData.layerId);
//         }

//         // 下拉选择框
//         int selectedIndex = EditorGUILayout.Popup("目标楼层", 0, layerOptions.ToArray());
//         if (selectedIndex > 0 && GUILayout.Button("🔄 更新选中楼层边界"))
//         {
//             int targetLayerId = layerIds[selectedIndex];
//             CalculateSingleLayerBounds(targetLayerId);
//         }

//         // 提示信息
//         EditorGUILayout.Space();
//         EditorGUILayout.HelpBox(
//             "说明：\n1. 边界基于GroundWall层Tilemap的cellBounds计算\n2. 计算后需手动保存场景（Ctrl+S）\n3. 空Tilemap会生成默认边界(1x1)", 
//             MessageType.Info
//         );
//     }

//     /// <summary>
//     /// 批量计算所有楼层的边界值
//     /// </summary>
//     private void CalculateAllLayersBounds()
//     {
//         if (_targetMapManager.allLayers.Count == 0)
//         {
//             EditorUtility.DisplayDialog("提示", "未配置任何楼层数据！", "确定");
//             return;
//         }

//         int successCount = 0;
//         int failCount = 0;
//         List<string> failLogs = new List<string>();

//         // 遍历所有楼层
//         foreach (var layerData in _targetMapManager.allLayers)
//         {
//             if (CalculateLayerBounds(layerData, out string errorMsg))
//             {
//                 successCount++;
//             }
//             else
//             {
//                 failCount++;
//                 failLogs.Add($"楼层 {layerData.layerId}：{errorMsg}");
//             }
//         }

//         // 显示计算结果
//         string resultMsg = $"批量计算完成！\n✅ 成功：{successCount} 层\n❌ 失败：{failCount} 层";
//         if (failLogs.Count > 0)
//         {
//             resultMsg += "\n\n失败详情：\n" + string.Join("\n", failLogs);
//         }
//         EditorUtility.DisplayDialog("计算结果", resultMsg, "确定");

//         // 刷新场景视图
//         SceneView.RepaintAll();
//     }

//     /// <summary>
//     /// 计算单个楼层的边界值
//     /// </summary>
//     /// <param name="targetLayerId">目标楼层ID</param>
//     private void CalculateSingleLayerBounds(int targetLayerId)
//     {
//         // 查找目标楼层数据
//         MapLayerInfo targetLayerData = _targetMapManager.allLayers.Find(l => l.layerId == targetLayerId);
//         if (targetLayerData == null)
//         {
//             EditorUtility.DisplayDialog("错误", $"未找到楼层ID为 {targetLayerId} 的配置！", "确定");
//             return;
//         }

//         // 计算边界
//         if (CalculateLayerBounds(targetLayerData, out string errorMsg))
//         {
//             EditorUtility.DisplayDialog("成功", $"楼层 {targetLayerId} 边界计算完成！", "确定");
//         }
//         else
//         {
//             EditorUtility.DisplayDialog("失败", $"楼层 {targetLayerId} 计算失败：{errorMsg}", "确定");
//         }

//         // 刷新场景视图
//         SceneView.RepaintAll();
//     }

//     /// <summary>
//     /// 核心：计算单个楼层数据的边界值并写入MapLayerInfo
//     /// </summary>
//     /// <param name="layerData">楼层数据</param>
//     /// <param name="errorMsg">错误信息（输出）</param>
//     /// <returns>是否成功</returns>
//     private bool CalculateLayerBounds(MapLayerInfo layerData, out string errorMsg)
//     {
//         errorMsg = string.Empty;

//         // 校验1：楼层根节点是否配置
//         if (layerData.layerRoot == null)
//         {
//             errorMsg = "楼层根节点（layerRoot）未配置！";
//             return false;
//         }

//         // 校验2：获取/创建MapLayerInfo组件
//         MapLayerInfo layerInfo = layerData.layerRoot.GetComponent<MapLayerInfo>();
//         if (layerInfo == null)
//         {
//             layerInfo = layerData.layerRoot.gameObject.AddComponent<MapLayerInfo>();
//             Debug.LogWarning($"楼层 {layerData.layerId} 缺少MapLayerInfo组件，已自动创建");
//         }

//         // 校验3：查找GroundWall Tilemap
//         Transform groundWallTrans = layerData.layerRoot.Find("GroundWall");
//         if (groundWallTrans == null)
//         {
//             errorMsg = "未找到名为 \"GroundWall\" 的子节点（基础层Tilemap容器）！";
//             return false;
//         }

//         Tilemap groundWallTilemap = groundWallTrans.GetComponent<Tilemap>();
//         if (groundWallTilemap == null)
//         {
//             errorMsg = "GroundWall节点下未挂载Tilemap组件！";
//             return false;
//         }

//         // 计算边界：优先使用有瓦片的真实边界，否则用默认边界
//         BoundsInt tilemapBounds = groundWallTilemap.cellBounds;
//         if (tilemapBounds.size.x <= 0 || tilemapBounds.size.y <= 0)
//         {
//             Debug.LogWarning($"楼层 {layerData.layerId} 的GroundWall层无瓦片，使用默认边界(0,0,0,1,1,1)");
//             tilemapBounds = new BoundsInt(0, 0, 0, 1, 1, 1);
//         }

//         // 写入边界值到MapLayerInfo
//         Undo.RecordObject(layerInfo, $"Update Layer {layerData.layerId} Bounds"); // 记录撤销操作
//         layerInfo.layerBounds = tilemapBounds;
//         layerInfo.layerId = layerData.layerId; // 同步楼层ID
//         layerInfo.layerRoot = layerData.layerRoot; // 同步根节点

//         // 同步到MapLayerInfo的隐藏字段（可选，保持数据一致）
//         Undo.RecordObject(_targetMapManager, $"Update Layer {layerData.layerId} Data Bounds");
//         layerData.layerBounds = tilemapBounds;

//         // 标记对象为已修改（确保保存）
//         EditorUtility.SetDirty(layerInfo);
//         EditorUtility.SetDirty(_targetMapManager);

//         return true;
//     }
// }