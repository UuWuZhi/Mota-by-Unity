using System;
using System.Collections.Generic;
using Modules.Enemy.DataDefine;
using Modules.Enemy.Runtime;
using Modules.Item.DataDefine;
using UnityEngine;
using VContainer;

// 纯 C# 实现的 MonsterBook 服务（用于 DI）
namespace Modules.Item.Runtime.MonsterBook
{
    public class MonsterBookService : IMonsterBook
    {
        private readonly Dictionary<int, PlayerSnapshot> _playerSnapshotByLayer = new();

        // layerId -> (enemyId -> predictedLoss)
        private readonly Dictionary<int, Dictionary<int, int>> _predictedLossByLayer = new();

        // layerId -> set of seen enemy ids
        private readonly Dictionary<int, HashSet<int>> _seenByLayer = new();
        private BattleManager _battleManager;
        private EnemyDatabase _enemyDatabase;

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
            if (_enemyDatabase)
            {
                var d = _enemyDatabase.GetById(enemyId);
                if (!d) Debug.LogWarning($"MonsterBookService: EnemyDatabase does not contain id {enemyId}");
                return d;
            }

            // No fallback to MonoBehaviour bridge available here; require _enemyDatabase registered in DI
            Debug.LogWarning($"MonsterBookService: No EnemyDatabase available to resolve id {enemyId}");
            return null;
        }

        public IEnumerable<int> GetSeenIdsForLayer(int layerId)
        {
            if (_seenByLayer.TryGetValue(layerId, out var set)) return set;
            return Array.Empty<int>();
        }

        public bool TryGetPredictedLoss(int layerId, int enemyId, out int loss)
        {
            loss = 0;
            if (_predictedLossByLayer.TryGetValue(layerId, out var map) && map != null)
                return map.TryGetValue(enemyId, out loss);
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
            _playerSnapshotByLayer[layerId] = new PlayerSnapshot { Attack = attack, Defense = defense };
        }

        public bool IsPlayerSnapshotSame(int layerId, int attack, int defense)
        {
            if (!_playerSnapshotByLayer.TryGetValue(layerId, out var snap) || snap == null) return false;
            return snap.Attack == attack && snap.Defense == defense;
        }

        [Inject]
        public void Inject(EnemyDatabase enemyDatabase, BattleManager battleManager)
        {
            _enemyDatabase = enemyDatabase;
            _battleManager = battleManager;
        }

        private class PlayerSnapshot
        {
            public int Attack;
            public int Defense;
        }
    }
}