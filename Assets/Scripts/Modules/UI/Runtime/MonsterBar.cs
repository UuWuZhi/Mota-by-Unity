using Modules.Enemy.DataDefine;
using Modules.Enemy.Runtime;
using Modules.Player.Runtime.Attribute;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace Modules.UI.Runtime
{
    /// <summary>
    ///     单个怪物展示条预制体的控制脚本（轻量）
    ///     绑定在 MonsterBar 预制体上，用于填充显示数据
    /// </summary>
    public class MonsterBar : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI textName;

        [FormerlySerializedAs("textHP")] [SerializeField]
        private TextMeshProUGUI textHp;

        [SerializeField] private TextMeshProUGUI textAttack;
        [SerializeField] private TextMeshProUGUI textDefense;
        [SerializeField] private TextMeshProUGUI textGold;
        [SerializeField] private TextMeshProUGUI textDamage;

        // injected (optional)
        private BattleManager _battleManager;
        private PlayerAttribute _playerAttribute;

        [Inject]
        public void Inject(BattleManager battleManager, PlayerAttribute playerAttribute)
        {
            _battleManager = battleManager;
            _playerAttribute = playerAttribute;
        }

        // 新增重载：如果外部已计算了 predictedLoss，则传入以避免重复计算
        public void SetData(EnemyData data, int? predictedLoss = null)
        {
            if (!data)
            {
                if (textName) textName.text = "Unknown";
                if (textHp) textHp.text = "-";
                if (textAttack) textAttack.text = "-";
                if (textDefense) textDefense.text = "-";
                if (textGold) textGold.text = "-";
                if (textDamage) textDamage.text = "-";
                return;
            }

            if (textName) textName.text = data.enemyName;
            if (textHp) textHp.text = data.maxHp.ToString();
            if (textAttack) textAttack.text = data.attack.ToString();
            if (textDefense) textDefense.text = data.defense.ToString();
            if (textGold) textGold.text = data.goldReward.ToString();

            // 显示预测的总HP损失（如果外部提供则使用外部值）
            if (!textDamage) return;
            if (predictedLoss.HasValue)
            {
                textDamage.text = predictedLoss.Value == int.MaxValue ? "∞" : predictedLoss.Value.ToString();
                return;
            }

            var bm = _battleManager;
            var pa = _playerAttribute;

            if (bm && pa)
            {
                var playerUnit = pa.GetPlayerUnitData();
                var enemyUnit = data.ToBattleUnitData();
                bm.ResolveBattle(playerUnit, enemyUnit, out var predicted);
                textDamage.text = predicted == int.MaxValue ? "∞" : predicted.ToString();
            }
            else
            {
                textDamage.text = "?";
            }
        }
    }
}