using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Modules.Enemy.DataDefine
{
    [CreateAssetMenu(fileName = "EnemyDatabase", menuName = "Data/Enemy/EnemyDatabase", order = 2)]
    public class EnemyDatabase : ScriptableObject
    {
        public List<EnemyData> enemies = new();

        private Dictionary<int, EnemyData> _index;

        private void OnEnable()
        {
            BuildIndex();
        }

        private void BuildIndex()
        {
            _index = new Dictionary<int, EnemyData>(enemies.Count);
            foreach (var e in enemies.Where(e => e)) _index.TryAdd(e.id, e);
        }

        public EnemyData GetById(int id)
        {
            if (_index == null) BuildIndex();
            EnemyData d = null;
            _index?.TryGetValue(id, out d);
            return d;
        }
    }
}