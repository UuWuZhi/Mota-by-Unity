using System.Collections.Generic;
using UnityEngine;
using VContainer;

// 纯 C# 实现的 MonsterBook 服务（用于 DI）
public class MonsterBookService : IMonsterBook
{
    private EnemyDatabase _enemyDatabase;
    private BattleManager _battleManager;

    // layerId -> set of seen enemy ids
    private readonly Dictionary<int, HashSet<int>> _seenByLayer = new Dictionary<int, HashSet<int>>();

    // layerId -> (enemyId -> predictedLoss)
    private readonly Dictionary<int, Dictionary<int, int>> _predictedLossByLayer = new Dictionary<int, Dictionary<int, int>>();

    private class PlayerSnapshot { public int attack; public int defense; }
    private readonly Dictionary<int, PlayerSnapshot> _playerSnapshotByLayer = new Dictionary<int, PlayerSnapshot>();

    [Inject]
    public void Inject(EnemyDatabase enemyDatabase, BattleManager battleManager)
    {
        _enemyDatabase = enemyDatabase;
        _battleManager = battleManager;
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
        if (_enemyDatabase != null)
        {
            var d = _enemyDatabase.GetById(enemyId);
            if (d == null) Debug.LogWarning($"MonsterBookService: EnemyDatabase does not contain id {enemyId}");
            return d;
        }
        // No fallback to MonoBehaviour bridge available here; require _enemyDatabase registered in DI
        Debug.LogWarning($"MonsterBookService: No EnemyDatabase available to resolve id {enemyId}");
        return null;
    }

    public IEnumerable<int> GetSeenIdsForLayer(int layerId)
    {
        if (_seenByLayer.TryGetValue(layerId, out var set)) return set;
        return new int[0];
    }

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
        _playerSnapshotByLayer[layerId] = new PlayerSnapshot { attack = attack, defense = defense };
    }

    public bool IsPlayerSnapshotSame(int layerId, int attack, int defense)
    {
        if (!_playerSnapshotByLayer.TryGetValue(layerId, out var snap) || snap == null) return false;
        return snap.attack == attack && snap.defense == defense;
    }
}
