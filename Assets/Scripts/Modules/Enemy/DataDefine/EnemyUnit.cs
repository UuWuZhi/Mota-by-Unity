using Modules.Core.Runtime;
using Modules.Enemy.Runtime;
using UnityEngine;

namespace Modules.Enemy.DataDefine
{
    /// <summary>
    ///     敌人单位组件：挂载在敌人 GameObject 上，存储战斗相关属性和扩展字段
    ///     可随项目需求继续扩展（速度、护甲、先攻、多段攻击、词条等）
    /// </summary>
    public class EnemyUnit : MonoBehaviour
    {
        [Header("基础战斗属性")] [Tooltip("关联的敌人数据（来自 EnemyDatabase 的 EnemyData 预制")]
        public EnemyData enemyData;

        public int EnemyID => enemyData ? enemyData.id : -1;

        /// <summary>
        ///     获取用于 BattleManager 计算的基础 BattleUnitData（会复制一份）
        ///     将来可以把复杂的词条/逻辑在此方法中折算为最终的 BattleUnitData
        /// </summary>
        public BattleUnitData GetBattleUnitData()
        {
            if (enemyData) return enemyData.ToBattleUnitData();

            DebugEditor.LogWarning("EnemyUnit 上未关联 EnemyData，返回默认 BattleUnitData");
            return new BattleUnitData
            {
                currentHp = 10,
                attack = 5,
                defense = 0
            };
        }
    }
}