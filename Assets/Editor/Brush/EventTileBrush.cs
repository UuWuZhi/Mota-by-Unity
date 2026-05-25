//using System.Collections.Generic;
//using UnityEditor;
//using UnityEditor.Tilemaps;
//using UnityEngine;
//using UnityEngine.Tilemaps;

///// <summary>
///// 编辑器用的 EventTile 刷新器，在编辑模式下刷新 EventTile 时自动在场景中生成对应的 prefab 实例并管理它们的层级关系。
///// 仅在 Editor 模式下生效，运行时无任何操作。
///// </summary>
//[CustomGridBrush(true, false, false, "EventTileBrush")]
//public class EventTileBrush : GridBrush
//{
//    public override void Paint(GridLayout grid, GameObject brushTarget, Vector3Int position)
//    {
//        if (Application.isPlaying) return;
//        base.Paint(grid, brushTarget, position);
//        SyncInstancesForTilemap(brushTarget);
//    }

//    public override void Erase(GridLayout grid, GameObject brushTarget, Vector3Int position)
//    {
//        if (Application.isPlaying) return;
//        base.Erase(grid, brushTarget, position);
//        SyncInstancesForTilemap(brushTarget);
//    }

//    public override void BoxFill(GridLayout grid, GameObject brushTarget, BoundsInt position)
//    {
//        if (Application.isPlaying) return;
//        base.BoxFill(grid, brushTarget, position);
//        SyncInstancesForTilemap(brushTarget);
//    }

//    public override void BoxErase(GridLayout grid, GameObject brushTarget, BoundsInt position)
//    {
//        if (Application.isPlaying) return;
//        base.BoxErase(grid, brushTarget, position);
//        SyncInstancesForTilemap(brushTarget);
//    }

//    public override void FloodFill(GridLayout grid, GameObject brushTarget, Vector3Int position)
//    {
//        if (Application.isPlaying) return;
//        base.FloodFill(grid, brushTarget, position);
//        SyncInstancesForTilemap(brushTarget);
//    }

//    // 同步当前 Tilemap（brushTarget）中 Tile 对应的 GameObject 实例
//    private void SyncInstancesForTilemap(GameObject brushTarget)
//    {
//        if (brushTarget == null) return;

//        Tilemap tilemap = brushTarget.GetComponent<Tilemap>();
//        if (tilemap == null) return;

//        if (Application.isPlaying) return;

//        // 修复：应使用 Tilemap 自身作为父节点（即 Event 节点），而不是其父对象 Layer_xx
//        Transform parent = tilemap.transform;

//        // 需要保留的实例 key 集合（用单元格坐标作为唯一标识）
//        HashSet<string> requiredKeys = new HashSet<string>();

//        // 遍历 tilemap 范围，检查 EventTile 并同步实例
//        foreach (Vector3Int cellPos in tilemap.cellBounds.allPositionsWithin)
//        {
//            if (!tilemap.HasTile(cellPos)) continue;

//            if (tilemap.GetTile(cellPos) is EventTile eventTile && eventTile != null)
//            {
//                GameObject prefab = eventTile.eventPrefab;
//                string key = GetKeyForCell(cellPos);
//                requiredKeys.Add(key);

//                // 直接在 Tilemap 的子节点中查找实例
//                Transform existing = parent.Find(key);
//                if (existing == null)
//                {
//                    // 实例化新对象，父节点设为 Tilemap
//                    if (prefab != null)
//                    {
//                        Object newObj = PrefabUtility.InstantiatePrefab(prefab, tilemap.gameObject.scene);
//                        if (newObj is GameObject go)
//                        {
//                            Undo.RegisterCreatedObjectUndo(go, "Create EventTile Instance");
//                            go.name = key;
//                            go.transform.SetParent(parent, true); // 父节点改为 Tilemap
//                            Vector3 worldPos = tilemap.GetCellCenterWorld(cellPos);
//                            go.transform.position = worldPos;
//                        }
//                    }
//                    else
//                    {
//                        DebugEditor.LogWarning($"EventTile 在单元格 {cellPos} 引用的 eventPrefab 为 null，未创建实例");
//                    }
//                }
//                else
//                {
//                    // 更新现有实例的位置
//                    Vector3 worldPos = tilemap.GetCellCenterWorld(cellPos);
//                    existing.position = worldPos;
//                }
//            }
//        }

//        // 清理 Tilemap 下不需要的实例（非 EventTile 对应的实例）
//        List<Transform> toRemove = new List<Transform>();
//        foreach (Transform child in parent) // 直接遍历 Tilemap 的子节点
//        {
//            if (!requiredKeys.Contains(child.name))
//            {
//                toRemove.Add(child);
//            }
//        }

//        foreach (var tr in toRemove)
//        {
//            Undo.DestroyObjectImmediate(tr.gameObject);
//        }
//    }

//    // 单元格坐标生成唯一标识（用于实例命名和查找）
//    private string GetKeyForCell(Vector3Int cellPos)
//    {
//        return $"Inst_{cellPos.x}_{cellPos.y}";
//    }
//}

