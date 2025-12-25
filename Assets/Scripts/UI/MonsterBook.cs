using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 怪物手册：保持已记录怪物信息（按层缓存）并提供查询/更新接口
/// 注意：依赖 EnemyDatabase 进行 id->EnemyData 查找
/// </summary>
public class MonsterBook : MonoBehaviour
{
    public static MonsterBook Instance { get; private set; }

    [Tooltip("引用的敌人数据库资源（在Inspector中配置）")]
    public EnemyDatabase enemyDatabase;

    // layerId -> set of seen enemy ids
    private Dictionary<int, HashSet<int>> _seenByLayer = new Dictionary<int, HashSet<int>>();

    // layerId -> (enemyId -> predictedLoss)
    private Dictionary<int, Dictionary<int, int>> _predictedLossByLayer = new Dictionary<int, Dictionary<int, int>>();

    // snapshot of player attributes when predictions were made
    private class PlayerSnapshot { public int attack; public int defense; }
    private Dictionary<int, PlayerSnapshot> _playerSnapshotByLayer = new Dictionary<int, PlayerSnapshot>();

    private void Awake()
    {
        if (Instance == null) Instance = this; else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    public void MarkSeen(int layerId, int enemyId)
    {
        if (!_seenByLayer.TryGetValue(layerId, out var set))
        {
            set = new HashSet<int>();
            _seenByLayer[layerId] = set;
        }
        set.Add(enemyId);
    }

    public bool HasSeen(int layerId, int enemyId)
    {
        return _seenByLayer.TryGetValue(layerId, out var set) && set.Contains(enemyId);
    }

    public EnemyData GetEnemyData(int enemyId)
    {
        if (enemyDatabase == null) return null;
        return enemyDatabase.GetById(enemyId);
    }

    public IEnumerable<int> GetSeenIdsForLayer(int layerId)
    {
        if (_seenByLayer.TryGetValue(layerId, out var set)) return set;
        return new int[0];
    }

    // Predicted loss API
    public bool TryGetPredictedLoss(int layerId, int enemyId, out int loss)
    {
        loss = 0;
        if (_predictedLossByLayer.TryGetValue(layerId, out var map) && map != null)
        {
            return map.TryGetValue(enemyId, out loss);
        }
        return false;
    }

    public void SetPredictedLoss(int layerId, int enemyId, int loss)
    {
        if (!_predictedLossByLayer.TryGetValue(layerId, out var map) || map == null)
        {
            map = new Dictionary<int, int>();
            _predictedLossByLayer[layerId] = map;
        }
        map[enemyId] = loss;
    }

    public void SetPlayerSnapshot(int layerId, int attack, int defense)
    {
        Debug.Log("设置玩家属性快照: 层 " + layerId + ", 攻击 " + attack + ", 防御 " + defense);
        _playerSnapshotByLayer[layerId] = new PlayerSnapshot { attack = attack, defense = defense };
    }

    public bool IsPlayerSnapshotSame(int layerId, int attack, int defense)
    {
        Debug.Log("检查玩家属性快照: 层 " + layerId + ", 攻击 " + attack + ", 防御 " + defense);
        if (!_playerSnapshotByLayer.TryGetValue(layerId, out var snap) || snap == null) return false;
        return snap.attack == attack && snap.defense == defense;
    }
}
