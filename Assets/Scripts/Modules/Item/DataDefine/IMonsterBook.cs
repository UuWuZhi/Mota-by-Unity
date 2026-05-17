using System.Collections.Generic;
using Modules.Enemy.DataDefine;

namespace Modules.Item.DataDefine
{
    public interface IMonsterBook
    {
        void MarkSeen(int layerId, int enemyId);
        bool HasSeen(int layerId, int enemyId);
        EnemyData GetEnemyData(int enemyId);
        IEnumerable<int> GetSeenIdsForLayer(int layerId);

        bool TryGetPredictedLoss(int layerId, int enemyId, out int loss);
        void SetPredictedLoss(int layerId, int enemyId, int loss);

        void SetPlayerSnapshot(int layerId, int attack, int defense);
        bool IsPlayerSnapshotSame(int layerId, int attack, int defense);
    }
}