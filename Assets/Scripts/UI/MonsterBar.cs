using UnityEngine;
using TMPro;
using VContainer;

/// <summary>
/// 单个怪物展示条预制体的控制脚本（轻量）
/// 绑定在 MonsterBar 预制体上，用于填充显示数据
/// </summary>
public class MonsterBar : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textName;
    [SerializeField] private TextMeshProUGUI textHP;
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
        _battleManager = battleManager ?? _battleManager;
        _playerAttribute = playerAttribute ?? _playerAttribute;
    }

    // 新增重载：如果外部已计算了 predictedLoss，则传入以避免重复计算
    public void SetData(EnemyData data, int? predictedLoss = null)
    {
        if (data == null)
        {
            if (textName != null) textName.text = "Unknown";
            if (textHP != null) textHP.text = "-";
            if (textAttack != null) textAttack.text = "-";
            if (textDefense != null) textDefense.text = "-";
            if (textGold != null) textGold.text = "-";
            if (textDamage != null) textDamage.text = "-";
            return;
        }

        if (textName != null) textName.text = data.enemyName;
        if (textHP != null) textHP.text = data.maxHP.ToString();
        if (textAttack != null) textAttack.text = data.attack.ToString();
        if (textDefense != null) textDefense.text = data.defense.ToString();
        if (textGold != null) textGold.text = data.goldReward.ToString();

        // 显示预测的总HP损失（如果外部提供则使用外部值）
        if (textDamage != null)
        {
            if (predictedLoss.HasValue)
            {
                textDamage.text = (predictedLoss.Value == int.MaxValue) ? "∞" : predictedLoss.Value.ToString();
                return;
            }

            // 使用注入的 manager/attribute 或回退到单例
            var bm = _battleManager ?? BattleManager.Instance;
            var pa = _playerAttribute;

            if (bm != null && pa != null)
            {
                var playerUnit = pa.GetPlayerUnitData();
                var enemyUnit = data.ToBattleUnitData();
                int predicted;
                bm.ResolveBattle(playerUnit, enemyUnit, out predicted);
                textDamage.text = (predicted == int.MaxValue) ? "∞" : predicted.ToString();
            }
            else
            {
                textDamage.text = "?";
            }
        }
    }
}
