using UnityEngine;
using TMPro; // 引入TextMeshPro命名空间
using VContainer;

public class UIAttribute : MonoBehaviour
{
    [Header("关联UI文本组件")]
    [SerializeField] private TextMeshProUGUI textHp;       // 显示血量的Text
    [SerializeField] private TextMeshProUGUI textAttack;   // 显示攻击的Text
    [SerializeField] private TextMeshProUGUI textDefense;  // 显示防御的Text
    [SerializeField] private TextMeshProUGUI textGold;     // 显示金币的Text

    private PlayerAttribute _playerAttribute;
    private EventCenter _eventCenter;

    private void OnEnable()
    {
        // 改为订阅注入的事件中心（若未注入则回退到单例）
        var ec = _eventCenter ?? EventCenter.Instance;
        if (ec != null)
        {
            ec.OnAttributeChanged += OnAttributeChanged;
        }
    }

    private void OnDisable()
    {
        var ec = _eventCenter ?? EventCenter.Instance;
        if (ec != null)
        {
            ec.OnAttributeChanged -= OnAttributeChanged;
        }
    }

    [Inject]
    public void Inject(PlayerAttribute playerAttribute, EventCenter eventCenter)
    {
        _playerAttribute = playerAttribute ?? _playerAttribute;
        _eventCenter = eventCenter ?? _eventCenter;
    }

    private void OnAttributeChanged(object sender, AttributeChangedEventArgs args)
    {
        UpdateUI(args.ChangedType);
    }


    // 【核心】更新所有属性的UI显示
    private void UpdateUI(AttributeType type)
    {
        switch (type)
        {
            case AttributeType.All: // 全量更新（所有UI）
                UpdateHpUI();
                UpdateAttackUI();
                UpdateDefenseUI();
                UpdateGoldUI();
                break;
            case AttributeType.HP: // 仅更新血量UI
                UpdateHpUI();
                break;

            case AttributeType.Attack: // 仅更新攻击UI
                UpdateAttackUI();
                break;

            case AttributeType.Defense: // 仅更新防御UI
                UpdateDefenseUI();
                break;
            case AttributeType.Gold:
                UpdateGoldUI();
                break;
            default:
                break;
        }
    }
    //单个UI的独立更新方法
    private void UpdateHpUI()
    {
        // 把“当前HP/最大HP”赋值给textHp文本
        if (textHp != null)
            textHp.text = $"{_playerAttribute.CurrentHP}";
    }

    private void UpdateAttackUI()
    {
        // 把“攻击数值”赋值给textAttack文本
        if (textAttack != null)
            textAttack.text = $"{_playerAttribute.Attack}";
    }

    private void UpdateDefenseUI()
    {
        // 把“防御数值”赋值给textDefense文本
        if (textDefense != null)
            textDefense.text = $"{_playerAttribute.Defense}";
    }
    private void UpdateGoldUI()
    {
        // 把“防御数值”赋值给textDefense文本
        if (textGold != null)
            textGold.text = $"{_playerAttribute.Gold}";
    }
}