using System.Collections.Generic;
using Modules.Core.DataDefine;
using Modules.Core.Runtime;
using Modules.Enemy.DataDefine;
using Modules.Enemy.Runtime;
using Modules.Item.DataDefine;
using Modules.Map.DataDefine.Tile;
using Modules.Map.Runtime;
using Modules.Player.Runtime.Attribute;
using UnityEngine;
using VContainer;

namespace Modules.UI.Runtime
{
    /// <summary>
    ///     怪物手册UI：在打开时收集当前层怪物ID并填充 MonsterBar 预制体
    ///     依赖 GridManager.CurrentEventTilemap 来获取当前层事件层 Tilemap
    /// </summary>
    public class MonsterBookUI : BaseUI
    {
        [Header("UI 元素")] public Transform contentParent; // 放 MonsterBar 的容器

        public GameObject monsterBarPrefab; // MonsterBar 预制

        private IGlobalEventVariables _globalEventVariables;
        private GridManager _gridManager;
        private IMonsterBook _monsterBook;
        private PlayerAttribute _playerAttribute;

        private int CurrentLayerId => _globalEventVariables.GetInt(GlobalEventKey.LayerId);

        #region 生命周期

        [Inject]
        public void Construct(IGlobalEventVariables globalEventVariables, GridManager gridManager,
            PlayerAttribute playerAttribute, IMonsterBook monsterBook)
        {
            _globalEventVariables = globalEventVariables;
            _gridManager = gridManager;
            _playerAttribute = playerAttribute;
            _monsterBook = monsterBook;
        }

        #endregion

        #region 事件系统

        // UI 显示由 UIManager 控制；重写 OnShown 在显示时刷新内容
        protected override void OnShown()
        {
            DebugEditor.Log("MonsterBookUI:进入显示函数");
            base.OnShown();
            Refresh();
        }

        #endregion

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
            if (!contentParent) return;
            foreach (Transform t in contentParent) Destroy(t.gameObject);
        }

        // 收集当前层所有怪物 ID 并标记已见
        private HashSet<int> CollectMonsterIds()
        {
            var ids = new HashSet<int>();
            var eventTilemap = _gridManager.CurrentEventTilemap;
            if (!eventTilemap) return ids;
            foreach (var cell in eventTilemap.cellBounds.allPositionsWithin)
            {
                if (!eventTilemap.HasTile(cell)) continue;
                if (eventTilemap.GetTile(cell) is not EventTile et || !et.gameObject) continue;
                var prefab = et.gameObject;
                var eu = prefab.GetComponent<EnemyUnit>();
                if (!eu || !eu.enemyData) continue;
                var id = eu.enemyData.id;
                ids.Add(id);
                _monsterBook?.MarkSeen(CurrentLayerId, id);
            }

            DebugEditor.Log($"MonsterBookUI:收集到怪物ID {string.Join(", ", ids)}");
            return ids;
        }

        // 基于玩家属性快照更新每个怪物的预测损失
        private void UpdatePredictions(HashSet<int> ids)
        {
            if (!_playerAttribute) return;
            var playerAtk = _playerAttribute.GetAttributeValue(AttributeType.Attack);
            var playerDef = _playerAttribute.GetAttributeValue(AttributeType.Defense);
            var snapshotSame = _monsterBook != null &&
                               _monsterBook.IsPlayerSnapshotSame(CurrentLayerId, playerAtk, playerDef);
            if (snapshotSame) return;
            foreach (var id in ids)
            {
                var data = _monsterBook?.GetEnemyData(id);
                if (!data) continue;
                BattleManager.Instance.ResolveBattle(_playerAttribute.GetPlayerUnitData(), data.ToBattleUnitData(),
                    out var predicted);
                _monsterBook?.SetPredictedLoss(CurrentLayerId, id, predicted);
            }

            _monsterBook?.SetPlayerSnapshot(CurrentLayerId, playerAtk, playerDef);
        }

        // 填充 UI 条目
        private void PopulateEntries(HashSet<int> ids)
        {
            if (!contentParent || !monsterBarPrefab) return;
            var sorted = new List<int>(ids);
            sorted.Sort();
            foreach (var id in sorted)
            {
                var data = _monsterBook?.GetEnemyData(id);
                var go = Instantiate(monsterBarPrefab, contentParent);
                var bar = go.GetComponent<MonsterBar>();
                if (bar && _monsterBook != null && _monsterBook.TryGetPredictedLoss(CurrentLayerId, id, out var loss))
                    bar.SetData(data, loss);
                else
                    bar.SetData(data);
            }
        }
    }
}