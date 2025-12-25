using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDatabase", menuName = "Data/EnemyDatabase", order = 2)]
public class EnemyDatabase : ScriptableObject
{
    public List<EnemyData> enemies = new List<EnemyData>();

    private Dictionary<int, EnemyData> _index;

    private void OnEnable()
    {
        BuildIndex();
    }

    private void BuildIndex()
    {
        _index = new Dictionary<int, EnemyData>(enemies.Count);
        foreach (var e in enemies)
        {
            if (e == null) continue;
            if (!_index.ContainsKey(e.id))
                _index.Add(e.id, e);
        }
    }

    public EnemyData GetById(int id)
    {
        if (_index == null) BuildIndex();
        _index.TryGetValue(id, out var d);
        return d;
    }
}
