using System.Collections.Generic;
using UnityEngine;
using VContainer;

/// <summary>
/// 怪物手册UI：在打开时收集当前层怪物ID并填充 MonsterBar 预制体
/// 依赖 GridManager.CurrentEventTilemap 来获取当前层事件层 Tilemap
/// </summary>
public class MonsterBookUI : MonoBehaviour
{
    [Header("UI 元素")]
    public GameObject root;
    public Transform contentParent; // 放 MonsterBar 的容器
    public GameObject monsterBarPrefab; // MonsterBar 预制

    private int _currentLayerId => _globalEventVariables.GetInt(GlobalEventKey.LayerId);

    private EventCenter _eventCenter;
    private UIManager _uiManager;
    private IGlobalEventVariables _globalEventVariables;
    private GridManager _gridManager;
    private PlayerAttribute _playerAttribute;

    private bool _registeredRoot = false;
    private bool _subscribed = false;
    private void OnEnable()
    {
        TryRegisterRoot();
        TrySubscribeShowEvent();
    }

    private void OnDisable()
    {
        TryUnregisterRoot();
        TryUnsubscribeShowEvent();
    }

    [Inject]
    public void Construct(EventCenter eventCenter, UIManager uiManager, IGlobalEventVariables globalEventVariables, GridManager gridManager, PlayerAttribute playerAttribute)
    {
        _eventCenter = eventCenter;
        _uiManager = uiManager;
        _globalEventVariables = globalEventVariables;
        _gridManager = gridManager;
        _playerAttribute = playerAttribute;
        TryRegisterRoot();
        TrySubscribeShowEvent();
    }

    private void TryRegisterRoot()
    {
        if (_registeredRoot) return;
        if (_uiManager == null || root == null) return;
        _uiManager.RegisterUIRoot(root);
        _registeredRoot = true;
    }

    private void TryUnregisterRoot()
    {
        if (!_registeredRoot) return;
        if (_uiManager != null && root != null) _uiManager.UnregisterUIRoot(root);
        _registeredRoot = false;
    }

    private void TrySubscribeShowEvent()
    {
        if (_subscribed) return;
        if (_eventCenter == null) return;
        _eventCenter.OnShowUI += OnShowUIHandler;
        _subscribed = true;
    }

    private void TryUnsubscribeShowEvent()
    {
        if (!_subscribed) return;
        if (_eventCenter != null) _eventCenter.OnShowUI -= OnShowUIHandler;
        _subscribed = false;
    }

    private void OnShowUIHandler(object sender, UIShowEventArgs args)
    {
        if (args == null) return;
        if (args.UINames != null && args.UINames.Contains("MonsterBook"))
        {
            Refresh();
        }
    }

    // NOTE: Show/Hide is handled centrally by UIManager / UIInputManager. Local event subscriptions limited to refresh on show.

    public void Refresh()
    {
        //Debug.Log("刷新怪物手册 UI");
        // 清理旧条目
        foreach (Transform t in contentParent) Destroy(t.gameObject);

        // 获取当前层所有怪物ID：通过 EventTilemap 的所有 EventTiles 的 eventPrefab 上的 EnemyUnit 中的 enemyData 或 id
        var eventTilemap = _gridManager.CurrentEventTilemap;
        HashSet<int> ids = new HashSet<int>();
        if (eventTilemap != null)
        {
            foreach (Vector3Int cell in eventTilemap.cellBounds.allPositionsWithin)
            {
                if (!eventTilemap.HasTile(cell)) continue;
                if (eventTilemap.GetTile(cell) is EventTile et && et.gameObject != null)
                {
                    var prefab = et.gameObject;
                    var eu = prefab.GetComponent<EnemyUnit>();
                    if (eu != null && eu.enemyData != null)
                    {
                        // 标记已见并收集 id
                        int id = eu.enemyData.id;
                        ids.Add(id);
                        MonsterBook.Instance.MarkSeen(_currentLayerId, id);
                    }
                }
            }
        }

        // 计算是否需要重新预测（基于玩家属性快照）
        var player = _playerAttribute;
        int playerAtk = player?.Attack ?? 0;
        int playerDef = player?.Defense ?? 0;
        //Debug.Log("当前玩家属性：攻击 " + playerAtk + " 防御 " + playerDef);
        bool snapshotSame = MonsterBook.Instance.IsPlayerSnapshotSame(_currentLayerId, playerAtk, playerDef);
        //Debug.Log("玩家属性快照是否相同：" + snapshotSame);
        // 若快照不同，需要为该层（或新出现的怪物）更新预测
        if (!snapshotSame)
        {
            // 更新每个怪物的预测并保存快照
            foreach (int id in ids)
            {
                var data = MonsterBook.Instance.GetEnemyData(id);
                if (data == null) continue;
                int predicted;
                BattleManager.Instance.ResolveBattle(player.GetPlayerUnitData(), data.ToBattleUnitData(), out predicted);
                MonsterBook.Instance.SetPredictedLoss(_currentLayerId, id, predicted);
            }
            MonsterBook.Instance.SetPlayerSnapshot(_currentLayerId, playerAtk, playerDef);
        }

        // 对收集到的 ids 进行排序并填充 UI（传入已计算的 predictedLoss）
        List<int> sorted = new List<int>(ids);
        sorted.Sort();
        foreach (int id in sorted)
        {
            var data = MonsterBook.Instance.GetEnemyData(id);
            var go = Instantiate(monsterBarPrefab, contentParent);
            var bar = go.GetComponent<MonsterBar>();
            int loss;
            if (bar != null && MonsterBook.Instance.TryGetPredictedLoss(_currentLayerId, id, out loss))
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
