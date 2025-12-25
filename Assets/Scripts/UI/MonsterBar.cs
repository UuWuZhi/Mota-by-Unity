using UnityEngine;
using TMPro;

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
                textDamage.text = (predictedLoss.Value == int.MaxValue) ? "—" : predictedLoss.Value.ToString();
                return;
            }

            // 否则回退到内部计算（保持兼容）
            if (BattleManager.Instance != null && PlayerAttribute.Instance != null)
            {
                var playerUnit = PlayerAttribute.Instance.GetPlayerUnitData();
                var enemyUnit = data.ToBattleUnitData();
                int predicted;
                BattleManager.Instance.ResolveBattle(playerUnit, enemyUnit, out predicted);
                textDamage.text = (predicted == int.MaxValue) ? "—" : predicted.ToString();
            }
            else
            {
                textDamage.text = "?";
            }
        }
    }
}
