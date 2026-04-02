using System.Collections.Generic;
using UnityEngine;
using VContainer;

/// <summary>
/// 怪物手册UI：在打开时收集当前层怪物ID并填充 MonsterBar 预制体
/// 依赖 GridManager.CurrentEventTilemap 来获取当前层事件层 Tilemap
/// </summary>
public class MonsterBookUI : BaseUI
{
    [Header("UI 元素")]
    public Transform contentParent; // 放 MonsterBar 的容器
    public GameObject monsterBarPrefab; // MonsterBar 预制

    private int CurrentLayerId => _globalEventVariables.GetInt(GlobalEventKey.LayerId);

    private IGlobalEventVariables _globalEventVariables;
    private GridManager _gridManager;
    private PlayerAttribute _playerAttribute;
    private IMonsterBook _monsterBook;

    //==============================================================================//
    //                                                                              //
    //                                 生命周期                                     //
    //                                                                              //
    //==============================================================================//
    #region 生命周期
    [Inject]
    public void Construct(IGlobalEventVariables globalEventVariables, GridManager gridManager, PlayerAttribute playerAttribute, IMonsterBook monsterBook)
    {
        _globalEventVariables = globalEventVariables;
        _gridManager = gridManager;
        _playerAttribute = playerAttribute;
        _monsterBook = monsterBook;
    }

    #endregion
    //==============================================================================//
    //                                                                              //
    //                                 事件系统                                     //
    //                                                                              //
    //==============================================================================//
    #region 事件系统
    // UI 显示由 UIManager 控制；重写 OnShown 在显示时刷新内容
    protected override void OnShown()
    {
        Debug.Log("MonsterBookUI:进入显示函数");
        base.OnShown();
        Refresh();
    }
    #endregion

    // NOTE: Show/Hide is handled centrally by UIManager / UIInputManager. Local event subscriptions limited to refresh on show.

    public void Refresh()
    {
        ClearContent();
        var ids = CollectMonsterIds();
        UpdatePredictions(ids);
        PopulateEntries(ids);
    }

    // 清理内容容器
    private void ClearContent()
    {
        if (contentParent == null) return;
        foreach (Transform t in contentParent)
        {
            Destroy(t.gameObject);
        }
    }

    // 收集当前层所有怪物 ID 并标记已见
    private HashSet<int> CollectMonsterIds()
    {
        var ids = new HashSet<int>();
        var eventTilemap = _gridManager.CurrentEventTilemap;
        if (eventTilemap == null) return ids;
        foreach (Vector3Int cell in eventTilemap.cellBounds.allPositionsWithin)
        {
            if (!eventTilemap.HasTile(cell)) continue;
            if (eventTilemap.GetTile(cell) is EventTile et && et.gameObject != null)
            {
                var prefab = et.gameObject;
                var eu = prefab.GetComponent<EnemyUnit>();
                if (eu != null && eu.enemyData != null)
                {
                    int id = eu.enemyData.id;
                    ids.Add(id);
                    _monsterBook?.MarkSeen(CurrentLayerId, id);
                }
            }
        }
        Debug.Log($"MonsterBookUI:收集到怪物ID {string.Join(", ", ids)}");
        return ids;
    }

    // 基于玩家属性快照更新每个怪物的预测损失
    private void UpdatePredictions(HashSet<int> ids)
    {
        if (_playerAttribute == null) return;
        int playerAtk = _playerAttribute.GetAttributeValue(AttributeType.Attack);
        int playerDef = _playerAttribute.GetAttributeValue(AttributeType.Defense);
        bool snapshotSame = _monsterBook != null && _monsterBook.IsPlayerSnapshotSame(CurrentLayerId, playerAtk, playerDef);
        if (snapshotSame) return;
        foreach (int id in ids)
        {
            var data = _monsterBook?.GetEnemyData(id);
            if (data == null) continue;
            int predicted;
            BattleManager.Instance.ResolveBattle(_playerAttribute.GetPlayerUnitData(), data.ToBattleUnitData(), out predicted);
            _monsterBook?.SetPredictedLoss(CurrentLayerId, id, predicted);
        }
        _monsterBook?.SetPlayerSnapshot(CurrentLayerId, playerAtk, playerDef);
    }

    // 填充 UI 条目
    private void PopulateEntries(HashSet<int> ids)
    {
        if (contentParent == null || monsterBarPrefab == null) return;
        var sorted = new List<int>(ids);
        sorted.Sort();
        foreach (int id in sorted)
        {
            var data = _monsterBook?.GetEnemyData(id);
            var go = Instantiate(monsterBarPrefab, contentParent);
            var bar = go.GetComponent<MonsterBar>();
            int loss;
            if (bar != null && _monsterBook != null && _monsterBook.TryGetPredictedLoss(CurrentLayerId, id, out loss))
            {
                bar.SetData(data, loss);
            }
            else
            {
                bar.SetData(data);
            }
        }
    }
}
