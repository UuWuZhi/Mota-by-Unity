using System;
using System.Collections.Generic;
using System.Linq; // 新增这行！解决FirstOrDefault的编译错误
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 地图楼层信息组件（挂载在楼层根节点，存储楼层边界和出生点）
/// </summary>
[RequireComponent(typeof(Transform))]
[Serializable]
public class MapLayerInfo : MonoBehaviour
{
    [Header("楼层基础信息")]
    public int layerId;                     // 楼层ID（与MapLayerData对应）
    public Tilemap spawnTilemap;            // 事件层Tilemap（用于事件系统查找）
    public Transform layerRoot;             // 该层的根节点（自身Transform）

    [Header("楼层边界数据")]
    [Tooltip("楼层边界数据（由编辑器工具自动计算）")]
    public BoundsInt layerBounds;           // 楼层网格边界

    /// <summary>
    /// 出生点数据结构（存储单个出生点的ID和位置）
    /// </summary>
    [Serializable]
    public class SpawnPoint
    {
        public int id;                      // 出生点ID（0：默认出生点，1：从上层下来的出生点等）
        public Vector2 position;            // 出生点世界坐标
    }

    [Tooltip("该楼层的所有出生点")]
    [SerializeField] private List<SpawnPoint> _spawnPoints = new List<SpawnPoint>();

    public List<SpawnPoint> SpawnPoints { get => _spawnPoints; set => _spawnPoints = value; }

    /// <summary>
    /// 根据ID获取出生点坐标
    /// </summary>
    /// <param name="id">出生点ID</param>
    /// <returns>出生点世界坐标（未找到返回Vector2.zero）</returns>
    public Vector2 GetSpawnPointById(int id)
    {
        var spawnPoint = _spawnPoints.Find(p => p.id == id);
        return spawnPoint != null ? spawnPoint.position : Vector2.zero;
    }

    /// <summary>
    /// 编辑模式下默认初始化（添加默认出生点）
    /// </summary>
    private void Reset()
    {
        if (_spawnPoints.Count == 0)
        {
            _spawnPoints.Add(new SpawnPoint { id = 0, position = transform.position });
            // _spawnPoints.Add(new SpawnPoint { id = 1, position = transform.position });
        }
    }
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_spawnPoints == null) return;

        // 绘制上楼梯出生点（红色方框）
        var upSpawn = _spawnPoints.FirstOrDefault(p => p.id == 0);
        if (upSpawn != null)
        {
            Gizmos.color = Color.red;
            // 绘制方框（中心位置为出生点，大小为1x1x0.1的立方体，视觉上为方框）
            Gizmos.DrawWireCube(upSpawn.position + new Vector2(0.5f, 0.5f), new Vector3(1f, 1f, 0.1f));
            UnityEditor.Handles.Label(upSpawn.position + Vector2.up * 0.3f, "UpSpawn (ID=0)");
        }

        // 绘制下楼梯出生点（蓝色方框）
        var downSpawn = _spawnPoints.FirstOrDefault(p => p.id == 1);
        if (downSpawn != null)
        {
            Gizmos.color = Color.blue;
            // 绘制方框（中心位置为出生点，大小为1x1x0.1的立方体，视觉上为方框）
            Gizmos.DrawWireCube(downSpawn.position + new Vector2(0.5f, 0.5f), new Vector3(1f, 1f, 0.1f));
            UnityEditor.Handles.Label(downSpawn.position + Vector2.up * 0.3f, "DownSpawn (ID=1)");
        }
    }
#endif
}